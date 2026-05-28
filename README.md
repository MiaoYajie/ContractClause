# ContractClause

合同起草 / 条款补全辅助系统，为 Agent 提供合同模板搜索、大纲生成、条款调取与补全能力。

## 技术栈

- ASP.NET Core 10 + DDD + MediatR (CQRS)
- PostgreSQL + Qdrant + OpenAI 兼容 Embeddings API
- REST API + MCP (stdio)

## 快速开始

### 1. 启动依赖

```bash
docker-compose up -d db qdrant
```

### 2. 配置

**开发环境（`ASPNETCORE_ENVIRONMENT=Development`）** 已提供 `appsettings.Development.json`（见仓库根目录 `appsettings.Development.example.json` 模板）。当前 dev 默认指向：

| 组件 | 地址 |
|------|------|
| PostgreSQL | `192.168.168.21:5432` / 库 `contractclause` / 用户 `fts-local` |
| Qdrant | `http://192.168.168.21:6333`（无 ApiKey） |

本地复制模板后填入密码：

```bash
copy appsettings.Development.example.json src\ContractClause.Api\appsettings.Development.json
copy appsettings.Development.example.json src\ContractClause.Mcp\appsettings.Development.json
```

生产或其它环境请编辑 `appsettings.json`，或设置环境变量 `Database__ConnectionString`、`VectorStore__Qdrant__Endpoint`。AI API Key 可选（无 Key 时降级为纯关键词搜索）。

### 3. 运行 API

```bash
dotnet run --project src/ContractClause.Api
```

- API 文档：http://localhost:5000/scalar
- 健康检查：http://localhost:5000/healthz

### 4. 创建 API Key

使用 `AuthServer`（OpenID Connect）签发的 Bearer access token；`ownerId` 取自 token 的 `sub` 声明。

```bash
curl -X POST http://localhost:5000/api/v1/apikeys \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d "{\"ownerType\":\"User\"}"
```

### 5. MCP 接入

#### 方式 A：本地 stdio（推荐）

```json
{
  "mcpServers": {
    "contractclause": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "D:/workbuddy/contractclause/src/ContractClause.Mcp/ContractClause.Mcp.csproj"
      ],
      "env": {
        "Mcp__Transport": "stdio",
        "MCP_API_KEY": "sk-xxxxxxxx"
      }
    }
  }
}
```

#### 方式 B：远程 HTTP（Streamable HTTP）

启动 HTTP 模式 MCP 服务：

```bash
# 本地
dotnet run --project src/ContractClause.Mcp --launch-profile http-sse

# 或 Docker
docker-compose up -d mcp-http
```

服务监听 `http://localhost:5001/mcp`（Docker 映射为 `5001:8080`）。

客户端配置（Cursor / 支持 Streamable HTTP 的 MCP Client）：

```json
{
  "mcpServers": {
    "contractclause": {
      "url": "http://localhost:5001/mcp",
      "headers": {
        "X-Api-Key": "sk-xxxxxxxx"
      }
    }
  }
}
```

| 配置项 | 说明 |
|--------|------|
| `Mcp:Transport` | `stdio`（默认）或 `http-sse` |
| `Mcp:HttpPort` | HTTP 模式端口，默认 5001 |
| `Mcp:HttpPath` | 端点路径前缀，默认 `/mcp` |

HTTP 模式在每次请求时校验 `X-Api-Key` 请求头，与 REST API 共用同一套 ApiKeys 表。

## 法天使模板元数据同步

详见 [模板元数据同步设计.md](./模板元数据同步设计.md)。API 启动后由 `TemplateSyncBackgroundService` 默认 **每 24 小时** 执行一次：

1. `GET https://contentadmin.fatianshi.cn/content/ForReview/FindUpdatedTemplates?size=50&page=0&recommend=true`
2. 以库中 `templates.SourceUpdatedAt` 最大值为水位；列表按 `PublishedOn` 倒序，遇 `PublishedOn <= 水位` 即停止翻页
3. 无水位时全量同步；字段映射写入 `templates`（含 `Alias`），并更新向量索引

配置（`FatianshiTemplateSync`）：

| 项 | 说明 |
|----|------|
| `ApiKey` | Bearer Token（必填，与设计文档一致） |
| `IntervalHours` | 默认 `24` |
| `PageSize` | 默认 `50`，page 从 0 起 |

HTML 正文导入仍走 `POST /api/v1/import/template`（条款入库）。已有库请执行 `scripts/migrate-fatianshi-sync.sql`。

## 项目结构

```
src/
  ContractClause.Domain/          # 领域实体
  ContractClause.Application/     # CQRS 应用层
  ContractClause.Infrastructure/  # EF Core、Qdrant、OpenAI
  ContractClause.Api/             # REST API
  ContractClause.Mcp/             # MCP 服务
```

详细设计见 [ContractClause设计文档.md](./ContractClause设计文档.md)。
