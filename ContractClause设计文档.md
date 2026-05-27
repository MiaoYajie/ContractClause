# ContractClause

合同起草 / 条款补全辅助系统，为 Agent 提供合同模板搜索、大纲生成、条款调取与补全能力。

---

## 目录

1. [需求概述](#需求概述)
2. [技术架构](#技术架构)
3. [混合搜索策略](#混合搜索策略)
4. [项目结构](#项目结构)
5. [数据加工流程](#数据加工流程)
6. [变量占位符规范](#变量占位符规范)
7. [实体设计](#实体设计)
8. [API 接口定义](#api-接口定义)
9. [MCP Tool 定义](#mcp-tool-定义)
10. [MCP 鉴权方案](#mcp-鉴权方案)
11. [配置说明](#配置说明)
12. [部署说明](#部署说明)
13. [日志与可观测性](#日志与可观测性)
14. [WorkBuddy MCP 接入指南](#workbuddy-mcp-接入指南)
15. [风险与待办](#风险与待办)

---

## 需求概述

将 HTML 格式的合同模板、合同条款片段清洗、抽取、切片转成 Markdown 文本后，入库并创建索引。

对外同时以 **API** 和 **MCP** 两种形式提供服务，包括：

| 功能 | 说明 |
|------|------|
| 搜索模板 | 支持语义 + 关键词混合搜索 |
| 获取大纲 | 按模板 ID 返回合同大纲树 |
| 获取条款 | 按合同类型 + 大纲节点 / 条款类型检索 |
| 获取模板变量 | 返回模板中所有占位变量 |
| 获取条款变量 | 返回指定条款中的占位变量 |
| 渲染合同全文 | 将模板 + 条款 + 变量值组装成完整合同 |

### 非功能性需求

| 维度 | 目标 | 说明 |
|------|------|------|
| 搜索响应时间 | P99 < 1s | 混合搜索（关键词 + 向量）端到端 |
| 单模板处理时间 | < 30s | 从 HTML 导入到双轨写入完成 |
| 并发 | 初始 50 QPS | 后续可按需水平扩展 |
| 数据规模 | 10,000 模板 / 500,000 条款 | 单 Qdrant 节点即可承载 |
| 可用性 | 99.9%（非核心系统） | 无跨 AZ 部署需求 |
| 错误容忍 | LLM / 向量库不可达时不阻塞 | 降级为纯关键词搜索或返回缓存数据 |

---

## 技术架构

### 核心框架

| 层次 | 技术选型 |
|------|----------|
| Web 框架 | ASP.NET Core (API + MCP) |
| 领域设计 | DDD + MediatR（CQRS） |
| ORM | EF Core |
| API 规范 | RESTful + Scalar / Swagger |
| MCP 服务 | ModelContextProtocol SDK for .NET |
| 日志 | Serilog + Seq / ELK |
| 健康检查 | ASP.NET Core Health Checks |

### 外部依赖（可配置）

| 类型 | 可选项 |
|------|--------|
| 关系库 | SQL Server / PostgreSQL |
| 向量库 | Qdrant / Azure AI Search |
| 向量模型 | 任意兼容 OpenAI Embeddings 接口的模型 |
| 文本模型 | 任意兼容 OpenAI Chat Completions 接口的模型 |

### 分层架构

```
┌─────────────────────────────────────────────┐
│          Presentation Layer                  │
│   ContractClause.Api  │  ContractClause.Mcp  │
├─────────────────────────────────────────────┤
│          Application Layer                   │
│          ContractClause.Application          │
│   Commands / Queries / DTOs / Handlers       │
│   ┌─────────────────────────────────────┐   │
│   │        IUserContext (鉴权上下文)      │   │
│   └─────────────────────────────────────┘   │
├─────────────────────────────────────────────┤
│           Domain Layer                       │
│           ContractClause.Domain              │
│   Aggregates / Entities / Domain Services    │
├─────────────────────────────────────────────┤
│        Infrastructure Layer                  │
│      ContractClause.Infrastructure           │
│  EF Core / Qdrant / Azure Search / OpenAI    │
└─────────────────────────────────────────────┘
```

---

## 混合搜索策略

全文搜索（关键词）与向量搜索（语义）的融合采用 **RRF（Reciprocal Rank Fusion）** 算法。

### 搜流程

```
用户输入 q
    │
    ├──→ 关键词搜索（PostgreSQL FTS / SQL Server Full-Text Index）
    │    对 Title + Summary + Text 做 tsquery 匹配
    │    输出：ranked list A
    │
    ├──→ 向量搜索（Qdrant / Azure AI Search）
    │    对 q 生成 embedding，在 template / clause 集合做 ANN 搜索
    │    输出：ranked list B
    │
    └──→ RRF 融合
         score = Σ(1 / (k + rank_i))
         k = 60（标准常量）
         去重后按融合分降序排列
```

### 降级策略

| 场景 | 行为 |
|------|------|
| 向量库不可达 | 纯关键词搜索，响应中标记 `"search_mode": "keyword_only"` |
| 关键词索引不存在 | 纯向量搜索 |
| 两者都不可达 | 返回 503，附带 `"retry_after"` 提示 |

---

## 项目结构

```
ContractClause/
├── src/
│   ├── ContractClause.Domain/               # 领域层（无外部依赖）
│   │   ├── Templates/
│   │   │   ├── Template.cs                  # 模板聚合根
│   │   │   ├── Outline.cs                   # 合同大纲值对象
│   │   │   └── OutlineItem.cs               # 大纲项值对象
│   │   ├── Clauses/
│   │   │   └── Clause.cs                    # 条款实体
│   │   ├── ApiKeys/
│   │   │   └── ApiKey.cs                    # ApiKey 聚合根
│   │   └── Shared/
│   │       └── ValueObjects/
│   │
│   ├── ContractClause.Application/          # 应用层（CQRS）
│   │   ├── Templates/
│   │   │   ├── Commands/
│   │   │   │   ├── ImportTemplate/
│   │   │   │   ├── UpdateTemplate/
│   │   │   │   ├── DeleteTemplate/
│   │   │   │   └── RenderContract/
│   │   │   └── Queries/
│   │   │       ├── SearchTemplates/
│   │   │       ├── GetOutline/
│   │   │       └── GetTemplateVariables/
│   │   ├── Clauses/
│   │   │   └── Queries/
│   │   │       ├── GetClauses/
│   │   │       └── GetClauseVariables/
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── ITemplateRepository.cs
│   │   │   │   ├── IClauseRepository.cs
│   │   │   │   ├── IVectorStore.cs
│   │   │   │   └── IUserContext.cs          # 鉴权上下文接口
│   │   │   └── DTOs/
│   │   └── DependencyInjection.cs
│   │
│   ├── ContractClause.Infrastructure/       # 基础设施层
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/              # EF Fluent API 配置
│   │   │   ├── Repositories/
│   │   │   └── Migrations/
│   │   ├── VectorStore/
│   │   │   ├── QdrantVectorStore.cs
│   │   │   └── AzureSearchVectorStore.cs
│   │   ├── Ai/
│   │   │   ├── OpenAiEmbeddingService.cs
│   │   │   └── OpenAiTextService.cs
│   │   └── DependencyInjection.cs
│   │
│   ├── ContractClause.Api/                  # REST API 宿主
│   │   ├── Controllers/
│   │   │   ├── TemplatesController.cs
│   │   │   ├── ClausesController.cs
│   │   │   ├── ImportController.cs
│   │   │   └── ApiKeysController.cs
│   │   ├── Middleware/
│   │   │   └── ApiKeyAuthMiddleware.cs
│   │   ├── appsettings.json
│   │   ├── Program.cs
│   │   └── Dockerfile
│   │
│   └── ContractClause.Mcp/                  # MCP 服务宿主
│       ├── Tools/
│       │   ├── SearchTemplatesTool.cs
│       │   ├── GetOutlineTool.cs
│       │   ├── GetClausesTool.cs
│       │   ├── GetTemplateVariablesTool.cs
│       │   ├── GetClauseVariablesTool.cs
│       │   └── RenderContractTool.cs
│       ├── Auth/
│       │   ├── McpAuthMiddleware.cs          # MCP 鉴权中间件
│       │   └── McpUserContext.cs             # MCP 用户上下文实现
│       ├── appsettings.json
│       ├── Program.cs
│       └── Dockerfile
│
├── tests/
│   ├── ContractClause.Domain.Tests/
│   ├── ContractClause.Application.Tests/
│   └── ContractClause.Infrastructure.Tests/
│
├── tools/
│   └── ContractClause.DataImport/           # 数据导入工具（CLI）
│       ├── Processors/
│       │   ├── HtmlCleaner.cs               # HTML → Markdown 清洗
│       │   ├── OutlineExtractor.cs          # 提取大纲
│       │   └── ClauseSegmenter.cs           # 条款切片
│       └── Program.cs
│
├── docker-compose.yml
├── docker-compose.override.yml
├── .env.template
└── ContractClause.sln
```

---

## 数据加工流程

### 模板加工

```
合同模板 (HTML)
    │
    ▼
① HTML 清洗
   保留：标题 / 段落 / 表格 / 下划线 / 加粗
   剥除：样式、脚本、注释、冗余属性
    │
    ▼
② Markdown 转换（Html2Markdown 或 正则+LLM）
    │
    ▼
③ 大纲抽取（LLM）
   输入：合同正文 Markdown
   输出：层级化 OutlineItem 树（含变量占位符）
   Prompt 策略：few-shot + 结构化 JSON 输出约束
   降级：若 LLM 输出格式不符 → 重试 2 次 → 降级为
         基于标题正则（# / ## / ###）的启发式抽取
    │
    ▼
④ 条款块切片
   按大纲节点的文本边界切分为独立 Clause 片段
   每个 Clause 继承其 OutlineItemId
    │
    ▼
⑤ 向量化（Embedding 模型）
   template 向量：Title + Summary + Scenarios 拼接 → 单个向量
   clause 向量：Text 前 512 tokens → 单个向量（分块策略待定）
    │
    ▼
⑥ 双轨写入（事务性保证）
   ┌────────────────────────┬─────────────────────────┐
   │  关系库（SQL）          │  向量库（Qdrant）         │
   │  template 行 INSERT    │  template 点 INSERT     │
   │  outline 行 INSERT     │  clause 点 INSERT       │
   │  clause 行 INSERT      │                         │
   │                        │                         │
   │  写失败 → 全部回滚      │  写失败 → 标记 VectorId    │
   │                        │  为空，后台重试            │
   └────────────────────────┴─────────────────────────┘
```

### 独立条款加工

```
条款片段 (HTML)
    │
    ▼
① HTML 清洗 → Markdown
    │
    ▼
② 变量识别（正则 + LLM）
   正则：匹配 {{...}} 模式
   LLM 补充：识别隐含变量（如日期、金额、名称）
    │
    ▼
③ 向量化 → 双轨写入
    Clause.TemplateId = null
    OutlineItemId = null
```

### 向量同步机制

| 操作 | 关系库 | 向量库 | 说明 |
|------|--------|--------|------|
| 创建 | INSERT | INSERT point | 写入后更新 Clause.VectorId |
| 更新 | UPDATE | UPDATE payload + re-embed | 重新生成向量并 upsert |
| 软删除 | SET IsDeleted=true | 不动 | 查询时过滤 IsDeleted |
| 物理删除 | DELETE / 定时清理 | DELETE point | 定时任务扫描 VectorId 做清理 |

---

## 变量占位符规范

| 规范 | 说明 |
|------|------|
| 格式 | `{{变量名}}` — 双花括号包裹 |
| 命名 | 中文，首字母大写：`{{甲方名称}}` / `{{合同金额}}` |
| 嵌套 | 不支持 — 扁平化变量列表 |
| 条件段落 | 不通过变量语法支持，由 LLM 渲染层处理 |
| 内置变量 | `{{合同日期}}` / `{{合同编号}}` — 由系统自动填充 |

变量以有序列表（非字典）形式传递，保留原始出现顺序。

---

## 实体设计

### ApiKey 表

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid (PK) | 主键 |
| OwnerId | Guid (FK) | 关联用户表 / 租户表 |
| OwnerType | string | 所有者类型（User / Tenant） |
| ApiKey | string | API Key 值（唯一索引） |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |
| IsDeleted | bool | 软删除标志 |
| CreatedBy | Guid | 创建者用户 ID |

---

### 模板元数据表（Templates）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid (PK) | 主键 |
| Number | int | 模板编号（唯一索引） |
| Title | string | 模板标题 |
| Type | string | 模板类型（合同 / 协议 / 备忘录等） |
| Categories | string[] | 模板分类（PG: TEXT[] / SQL Server: JSON 列或关联表） |
| Tags | string[] | 模板标签（同上） |
| Summary | string | 模板摘要（用于向量化及展示） |
| Scenarios | string | 适用场景描述 |
| IsOfficial | bool | 是否官方模板 |
| OwnerId | Guid? | 所属用户 / 租户，可空表示公共模板 |
| Version | int | 版本号（乐观锁） |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |
| IsDeleted | bool | 软删除标志 |

> **Categories / Tags 存储说明**：PostgreSQL 使用 `TEXT[]` 原生数组类型；SQL Server 使用 JSON 列 `NVARCHAR(MAX)`，通过 EF Core JSON 映射读写。

---

### 合同大纲表（Outlines）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid (PK) | 主键 |
| TemplateId | Guid (FK) | 关联模板元数据表（一对一） |
| OutlineJson | string | 大纲 JSON 全文（序列化 OutlineItem 树） |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |

> **性能说明**：大纲存为 JSON 字符串，不支持对单个大纲项的关系查询。如需按大纲项维度搜索（如"包含变量 X 的所有大纲项"），需要反序列化后筛选。模板级别查询（按 TemplateId 获取整棵树）不受影响。

#### 大纲项（OutlineItem，非物理表，存于 OutlineJson）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | string | 大纲项 ID（如 "1.2.3"） |
| Title | string | 大纲项标题 |
| Level | int | 层级深度（1 = 一级标题） |
| Variables | string[] | 该节点的变量占位符列表 |
| Children | OutlineItem[] | 子大纲项 |

---

### 条款表（Clauses）

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid (PK) | 主键 |
| TemplateId | Guid? (FK) | 关联模板，可空（独立条款库的条目无 TemplateId） |
| OutlineItemId | string? | 关联大纲项 ID（对应 OutlineItem.Id，详见说明） |
| Text | string | 条款 Markdown 正文 |
| Variables | string[] | 条款内变量占位符列表 |
| ClauseType | string | 条款类型（义务 / 权利 / 定义 / 声明 / 争议解决等） |
| Keywords | string[] | 条款关键词（用于关键词搜索） |
| VectorId | string? | 向量库中对应的向量点 ID（为空表示异步重试中） |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime | 更新时间 |
| IsDeleted | bool | 软删除标志 |

> **说明**：
> - `OutlineItemId` 为字符串类型，关联大纲项 JSON 树中的逻辑 ID（非数据库外键约束），因为大纲项存储在 Outlines 表的 JSON 列中。
> - 独立条款 `<Clause>`（无 TemplateId）的 `OutlineItemId` 永远为 null，搜索时注意排除。

---

### 向量存储（非关系型，Qdrant / Azure AI Search）

#### template 集合

| 字段 | 说明 |
|------|------|
| id | 与关系库 Templates.Id 对应 |
| vector | 由 Title + Summary + Scenarios 合并生成的向量 |
| payload.title | 模板标题 |
| payload.type | 模板类型 |
| payload.categories | 分类标签 |
| payload.isOfficial | 是否官方 |

#### clause 集合

| 字段 | 说明 |
|------|------|
| id | 与关系库 Clauses.Id 对应 |
| vector | 由 Text 前 512 tokens 生成的向量 |
| payload.templateId | 所属模板 ID |
| payload.clauseType | 条款类型 |
| payload.keywords | 关键词 |

---

## API 接口定义

所有接口路由前缀：`/api/v1`

鉴权方式：请求头 `X-Api-Key: <key>`

### 统一错误响应格式

所有错误响应使用以下格式：

```json
{
  "code": "TEMPLATE_NOT_FOUND",
  "message": "模板 3fa85f64-... 不存在",
  "details": {}
}
```

| HTTP 状态码 | 说明 |
|-------------|------|
| 401 | `X-Api-Key` 缺失或无效 |
| 404 | 资源不存在（模板 / 条款未找到） |
| 400 | 请求参数校验失败 |
| 500 | 内部服务错误 |
| 503 | 服务暂不可用（依赖故障） |

---

### 模板接口

#### 搜索模板

```
GET /api/v1/templates/search
```

**Query 参数**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| q | string | 是 | 搜索关键词 / 语义描述 |
| type | string | 否 | 模板类型过滤 |
| categories | string[] | 否 | 分类过滤 |
| isOfficial | bool | 否 | 是否只看官方模板 |
| sort | string | 否 | 排序方式：`relevance`（默认）/ `created_at` / `title` |
| page | int | 否 | 页码，默认 1 |
| pageSize | int | 否 | 每页数量，默认 10，最大 50 |

**响应**

```json
{
  "total": 42,
  "page": 1,
  "pageSize": 10,
  "searchMode": "hybrid",
  "items": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "number": 101,
      "title": "软件开发服务合同",
      "type": "合同",
      "categories": ["IT服务", "软件开发"],
      "tags": ["软件", "外包", "服务"],
      "summary": "适用于软件开发外包场景的标准合同模板...",
      "isOfficial": true,
      "score": 0.92
    }
  ]
}
```

> `searchMode` 字段标记本次搜索模式：`hybrid` / `keyword_only` / `vector_only`，用于降级时告知调用方。

---

#### 获取模板大纲

```
GET /api/v1/templates/{id}/outline
```

**响应**

```json
{
  "templateId": "3fa85f64-...",
  "outline": [
    {
      "id": "1",
      "title": "第一条 定义",
      "level": 1,
      "variables": [],
      "children": [
        {
          "id": "1.1",
          "title": "1.1 甲方定义",
          "level": 2,
          "variables": ["{{甲方名称}}", "{{甲方地址}}"],
          "children": []
        }
      ]
    }
  ]
}
```

---

#### 获取模板变量

```
GET /api/v1/templates/{id}/variables
```

**响应**

```json
{
  "templateId": "3fa85f64-...",
  "variables": [
    { "name": "{{甲方名称}}", "description": "合同甲方的完整公司名称", "required": true },
    { "name": "{{合同金额}}", "description": "合同总金额（人民币）", "required": true }
  ]
}
```

---

#### 更新模板

```
PUT /api/v1/templates/{id}
```

**请求体**

```json
{
  "title": "更新后的标题",
  "type": "合同",
  "categories": ["IT服务"],
  "tags": ["软件"],
  "summary": "更新后的摘要",
  "scenarios": "更新后的适用场景"
}
```

**响应**: 204 No Content

> 更新模板元数据后，向量库中的 payload 需要同步更新（重新 upsert）。

---

#### 删除模板

```
DELETE /api/v1/templates/{id}
```

**响应**: 204 No Content（软删除）

> 软删除：`IsDeleted = true`。关联的 clause 行标记相同。向量库中的点保留不动（查询时过滤）。

---

#### 渲染合同全文

```
POST /api/v1/templates/{id}/render
```

**请求体**

```json
{
  "variables": {
    "{{甲方名称}}": "北京科技有限公司",
    "{{乙方名称}}": "上海软件开发有限公司",
    "{{合同金额}}": "100,000"
  },
  "format": "markdown"
}
```

**响应**

```json
{
  "templateId": "3fa85f64-...",
  "format": "markdown",
  "content": "# 软件开发服务合同\n\n甲方：北京科技有限公司..."
}
```

---

### 条款接口

#### 搜索条款

```
GET /api/v1/clauses/search
```

**Query 参数**

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| q | string | 是 | 搜索描述 |
| templateId | Guid | 否 | 限定在某模板内搜索 |
| clauseType | string | 否 | 条款类型过滤 |
| page | int | 否 | 页码，默认 1 |
| pageSize | int | 否 | 每页数量，默认 10 |

**响应**

```json
{
  "total": 15,
  "items": [
    {
      "id": "abc12345-...",
      "templateId": "3fa85f64-...",
      "outlineItemId": "3.2",
      "clauseType": "义务",
      "text": "## 第三条 乙方义务\n\n3.2 乙方应按时交付...",
      "variables": ["{{交付日期}}"],
      "keywords": ["交付", "义务", "时间"],
      "score": 0.88
    }
  ]
}
```

---

#### 获取条款变量

```
GET /api/v1/clauses/{id}/variables
```

**响应**

```json
{
  "clauseId": "abc12345-...",
  "variables": [
    { "name": "{{交付日期}}", "description": "软件交付的截止日期", "required": true }
  ]
}
```

---

### 数据导入接口

#### 导入模板

```
POST /api/v1/import/template
Content-Type: multipart/form-data
```

**表单字段**

| 字段 | 类型 | 说明 |
|------|------|------|
| file | file | HTML 格式的合同模板文件 |
| title | string | 模板标题 |
| type | string | 模板类型 |
| categories | string | 分类（逗号分隔） |
| tags | string | 标签（逗号分隔） |
| isOfficial | bool | 是否官方模板 |

> categories / tags 在上传时按逗号分隔字符串传入，后端解析为 `string[]`。

**响应**

```json
{
  "taskId": "import-task-uuid",
  "status": "processing",
  "message": "模板导入任务已提交，正在后台处理"
}
```

#### 查询导入任务状态

```
GET /api/v1/import/tasks/{taskId}
```

**响应**

```json
{
  "taskId": "import-task-uuid",
  "status": "completed",
  "templateId": "3fa85f64-...",
  "clausesImported": 48,
  "errors": []
}
```

> 建议轮询间隔：> 2 秒。导入流程耗时主要取决于 LLM 调用（抽取大纲），通常在 10–30 秒内完成。

---

## MCP Tool 定义

MCP 服务使用 `ModelContextProtocol` SDK，以 stdio 或 HTTP SSE 两种传输方式提供。

### Tool 列表

#### `search_templates`

搜索合同模板（语义 + 关键词混合）。

**输入**

```json
{
  "query": {
    "type": "string",
    "description": "搜索关键词或对合同场景的自然语言描述",
    "required": true
  },
  "type": {
    "type": "string",
    "description": "模板类型过滤，如：合同、协议、备忘录",
    "required": false
  },
  "categories": {
    "type": "array",
    "items": { "type": "string" },
    "description": "分类过滤",
    "required": false
  },
  "limit": {
    "type": "integer",
    "description": "返回数量，默认 5，最大 20",
    "required": false
  }
}
```

**输出**

```json
{
  "content": [
    {
      "type": "text",
      "text": "[{\"id\":\"3fa85f64-...\",\"number\":101,\"title\":\"软件开发服务合同\",\"summary\":\"适用于软件开发外包场景...\",\"score\":0.92}]"
    }
  ],
  "isError": false
}
```

> MCP 输出遵循 MCP 协议标准格式。超过 limit 的截断结果不返回（调用方可缩小条件重新查询）。

---

#### `get_outline`

获取指定模板的合同大纲。

**输入**

```json
{
  "template_id": {
    "type": "string",
    "description": "模板 ID（Guid）",
    "required": true
  }
}
```

**输出**

```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"templateId\":\"3fa85f64-...\",\"outline\":[...]}"
    }
  ],
  "isError": false
}
```

---

#### `get_clauses`

按条件获取合同条款。

**输入**

```json
{
  "query": {
    "type": "string",
    "description": "对所需条款的语义描述",
    "required": false
  },
  "template_id": {
    "type": "string",
    "description": "限定在某模板内搜索",
    "required": false
  },
  "outline_item_id": {
    "type": "string",
    "description": "指定大纲项 ID，获取该节点下的条款",
    "required": false
  },
  "clause_type": {
    "type": "string",
    "description": "条款类型，如：义务、权利、定义、声明、争议解决",
    "required": false
  },
  "limit": {
    "type": "integer",
    "description": "返回数量，默认 5，最大 20",
    "required": false
  }
}
```

**输出**

```json
{
  "content": [
    {
      "type": "text",
      "text": "[{\"id\":\"abc12345-...\",\"outlineItemId\":\"3.2\",\"clauseType\":\"义务\",\"text\":\"## 第三条 乙方义务\\n3.2 乙方应按时交付...\",\"variables\":[\"{{交付日期}}\"]}]"
    }
  ],
  "isError": false
}
```

---

#### `get_template_variables`

获取模板的所有变量占位符及说明。

**输入**

```json
{
  "template_id": {
    "type": "string",
    "description": "模板 ID（Guid）",
    "required": true
  }
}
```

**输出**

```json
{
  "content": [
    {
      "type": "text",
      "text": "[{\"name\":\"{{甲方名称}}\",\"description\":\"合同甲方的完整公司名称\",\"required\":true},{\"name\":\"{{合同金额}}\",\"description\":\"合同总金额（人民币）\",\"required\":true}]"
    }
  ],
  "isError": false
}
```

---

#### `get_clause_variables`

获取指定条款的变量占位符。

**输入**

```json
{
  "clause_id": {
    "type": "string",
    "description": "条款 ID（Guid）",
    "required": true
  }
}
```

**输出**

```json
{
  "content": [
    {
      "type": "text",
      "text": "[{\"name\":\"{{交付日期}}\",\"description\":\"软件交付的截止日期\",\"required\":true}]"
    }
  ],
  "isError": false
}
```

---

#### `render_contract`

将模板与变量值组装，渲染为完整合同正文。

**输入**

```json
{
  "template_id": {
    "type": "string",
    "description": "模板 ID（Guid）",
    "required": true
  },
  "variables": {
    "type": "object",
    "description": "变量名到值的映射，如 {\"{{甲方名称}}\": \"北京科技有限公司\"}",
    "required": true
  }
}
```

**输出**

```json
{
  "content": [
    {
      "type": "text",
      "text": "{\"content\":\"# 软件开发服务合同\\n\\n...\",\"missingVariables\":[]}"
    }
  ],
  "isError": false
}
```

---

## MCP 鉴权方案

### 背景

MCP 服务同时提供 stdio（本地进程）和 HTTP-SSE（远程访问）两种传输模式，两种模式的鉴权策略不同。同时，MCP 工具在执行时需要在调用链中携带用户上下文（OwnerId），以实现与 REST API 一致的多租户数据隔离。

### 设计原则

1. **鉴权在传输层完成** — MCP Tool 本身不感知鉴权细节，由传输中间件处理。
2. **用户上下文在应用层传递** — 鉴权通过后，`OwnerId` 通过 `IUserContext` 注入 Application 层。
3. **API 与 MCP 共用同一鉴权体系** — 都使用 `ApiKeys` 表做凭证校验，避免两套身份体系。

### 整体架构

```
┌──────────────────────────────────────────────────────────────────┐
│                       MCP Client (Agent)                         │
│  stdio: ENV MCP_API_KEY=sk-xxx                                   │
│  HTTP:  Header X-Api-Key: sk-xxx                                 │
└──────────┬───────────────────────────────┬───────────────────────┘
           │                               │
     stdio mode                      HTTP-SSE mode
           │                               │
           ▼                               ▼
┌──────────────────────┐  ┌──────────────────────────────────────┐
│  McpAuthMiddleware    │  │  McpAuthMiddleware (HTTP pipeline)   │
│  (启动时读取 ENV)      │  │  (拦截 SSE 建立请求，校验 X-Api-Key)   │
│                      │  │                                       │
│  读取 MCP_API_KEY     │  │  读取 X-Api-Key → 查 ApiKeys 表       │
│  → 查 ApiKeys 表      │  │  → 解析 OwnerId / OwnerType          │
│  → 解析 OwnerId      │  │  → 创建 MCP Session（关联 OwnerId）   │
│  → 注入 IUserContext  │  │  → 后续 Tool 调用自动携带             │
└──────────────────────┘  └──────────────────────────────────────┘
           │                               │
           └───────────┬───────────────────┘
                       ▼
          ┌──────────────────────────┐
          │   IUserContext            │
          │   OwnerId: Guid?          │
          │   OwnerType: string?      │
          │   IsAuthenticated: bool   │
          └──────────┬───────────────┘
                     │ (注入 MediatR Pipeline)
                     ▼
          ┌──────────────────────────┐
          │    Application Layer      │
          │  (查询自动过滤 OwnerId)    │
          └──────────────────────────┘
```

### 方案详解

#### stdio 模式

**工作流程：**

1. Agent 启动 MCP 子进程时，通过环境变量 `MCP_API_KEY` 传入 API Key
2. MCP 服务启动时，`McpAuthMiddleware` 读取该环境变量
3. 查询 `ApiKeys` 表校验 Key 有效、`IsDeleted = false`
4. 校验通过 → 解析 `OwnerId` / `OwnerType` → 注入全局 `IUserContext`
5. 校验失败 → 进程启动失败，返回错误给 Agent

**Agent (Claude Desktop / Cursor) 配置示例：**

```json
{
  "mcpServers": {
    "contractclause": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "src/ContractClause.Mcp/ContractClause.Mcp.csproj"
      ],
      "env": {
        "MCP_API_KEY": "sk-xxxxxxxxxxxxxxxx"
      }
    }
  }
}
```

#### HTTP-SSE 模式

**工作流程：**

1. Agent 向 MCP 服务 `POST /mcp/sse` 发起 SSE 连接
2. 请求头必须包含 `X-Api-Key: sk-xxx`
3. MCP HTTP 中间件校验 Key，查询 `ApiKeys` 表
4. 校验通过 → 创建 Session（关联 OwnerId），建立 SSE 长连接
5. 该校验在 SSE 连接建立时**一次性执行**，后续 Tool 调用共享 Session 上下文
6. 校验失败 → 返回 `401 Unauthorized`

**Agent 配置示例：**

```json
{
  "mcpServers": {
    "contractclause": {
      "url": "http://mcp.example.com/mcp/sse",
      "headers": {
        "X-Api-Key": "sk-xxxxxxxxxxxxxxxx"
      }
    }
  }
}
```

### 应用层集成（IUserContext）

MCP 和 API 共享同一个 `IUserContext` 接口：

```csharp
// ContractClause.Application/Common/Interfaces/IUserContext.cs
public interface IUserContext
{
    Guid? OwnerId { get; }
    string? OwnerType { get; }   // "User" | "Tenant" | null (公共模板)
    bool IsAuthenticated { get; }
}
```

**实现类：**

| 实现 | 所在项目 | 使用场景 |
|------|----------|----------|
| `McpUserContext` | ContractClause.Mcp | 通过环境变量（stdio）或 Session（HTTP）注入 |
| `ApiUserContext` | ContractClause.Api | 通过 `X-Api-Key` 中间件解析注入 |
| `SystemUserContext` | (仅导入工具) | 数据导入 CLI，OwnerId = System |

**数据隔离逻辑（自动过滤）：**

- `OwnerId = null` 的模板 = 公共模板，**对所有用户可见**
- 登录用户的查询结果 = `公共模板 WHERE IsOfficial=true OR OwnerId=null` **+** `自己的模板 WHERE OwnerId=当前用户`
- `IUserContext` 通过 MediatR 的 `IPipelineBehavior` 自动注入到所有 QueryHandler 中

### 代码实现示意

```csharp
// McpAuthMiddleware.cs — stdio 模式
public class McpAuthMiddleware
{
    private readonly string _mcpApiKey;

    public McpAuthMiddleware(IConfiguration config)
    {
        _mcpApiKey = config["MCP_API_KEY"]
            ?? throw new InvalidOperationException("MCP_API_KEY 环境变量未设置");
    }

    public async Task<IUserContext> AuthenticateAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apiKey = await db.ApiKeys
            .FirstOrDefaultAsync(k => k.ApiKey == _mcpApiKey && !k.IsDeleted);
        if (apiKey == null)
            throw new UnauthorizedAccessException("API Key 无效");

        return new McpUserContext
        {
            OwnerId = apiKey.OwnerId,
            OwnerType = apiKey.OwnerType,
            IsAuthenticated = true
        };
    }
}
```

```csharp
// Http MCP 中间件 — HTTP-SSE 模式
public class McpHttpAuthMiddleware
{
    public async Task InvokeAsync(HttpContext context, Func<Task> next)
    {
        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("{\"error\":\"Missing X-Api-Key\"}");
            return;
        }

        // 校验 Key 并注入 IUserContext 到请求范围
        var userContext = await ValidateAndCreateUserContext(apiKey!);
        if (userContext == null)
        {
            context.Response.StatusCode = 401;
            return;
        }

        context.Items["UserContext"] = userContext;
        await next();
    }
}
```

### 鉴权对照总结

| 场景 | 鉴权方式 | 凭证位置 | 校验时机 | 失败行为 |
|------|----------|----------|----------|----------|
| API REST | 请求头 `X-Api-Key` | HTTP Header | 每次请求 | 401 |
| MCP stdio | 环境变量 `MCP_API_KEY` | 进程 ENV | 服务启动时 | 进程启动失败 |
| MCP HTTP-SSE | 请求头 `X-Api-Key` | HTTP Header | SSE 连接建立时 | 401，连接拒绝 |
| 数据导入 CLI | 环境变量 `CC_API_KEY` | 进程 ENV | 启动时 | 退出码 1 |

---

## 配置说明

### appsettings.json 结构

```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "ConnectionString": "Host=localhost;Database=contractclause;Username=app;Password=..."
  },
  "VectorStore": {
    "Provider": "Qdrant",
    "Qdrant": {
      "Endpoint": "http://localhost:6333",
      "ApiKey": ""
    },
    "AzureSearch": {
      "Endpoint": "https://<service>.search.windows.net",
      "ApiKey": ""
    }
  },
  "Ai": {
    "EmbeddingModel": {
      "BaseUrl": "https://api.openai.com/v1",
      "ApiKey": "",
      "ModelId": "text-embedding-3-small",
      "Dimensions": 1536
    },
    "TextModel": {
      "BaseUrl": "https://api.openai.com/v1",
      "ApiKey": "",
      "ModelId": "gpt-4o-mini"
    }
  },
  "Mcp": {
    "Transport": "stdio",
    "HttpPort": 5001
  }
}
```

### 配置说明

| 键 | 可选值 | 说明 |
|----|--------|------|
| Database.Provider | `SqlServer` / `PostgreSQL` | 关系库类型 |
| VectorStore.Provider | `Qdrant` / `AzureSearch` | 向量库类型 |
| Mcp.Transport | `stdio` / `http-sse` | MCP 传输方式 |

---

## 部署说明

### MCP 服务说明

> MCP 服务是独立进程，直接引用 Application 和 Infrastructure 层，**不依赖 API 服务**。stdio 模式下被 Agent 直接拉起；HTTP-SSE 模式下独立运行于容器中。

### Docker Compose

```yaml
version: '3.9'
services:
  api:
    build:
      context: .
      dockerfile: src/ContractClause.Api/Dockerfile
    ports:
      - "5000:8080"
    environment:
      - Database__Provider=PostgreSQL
      - Database__ConnectionString=Host=db;Database=contractclause;Username=app;Password=secret
      - VectorStore__Provider=Qdrant
      - VectorStore__Qdrant__Endpoint=http://qdrant:6333
      - Ai__EmbeddingModel__ApiKey=${EMBEDDING_API_KEY}
      - Ai__TextModel__ApiKey=${TEXT_API_KEY}
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/healthz"]
      interval: 30s
      timeout: 10s
      retries: 3
    depends_on:
      db:
        condition: service_healthy
      qdrant:
        condition: service_healthy

  mcp-http:
    build:
      context: .
      dockerfile: src/ContractClause.Mcp/Dockerfile
    ports:
      - "5001:8080"
    environment:
      - Mcp__Transport=http-sse
      - Database__Provider=PostgreSQL
      - Database__ConnectionString=Host=db;Database=contractclause;Username=app;Password=secret
      - VectorStore__Provider=Qdrant
      - VectorStore__Qdrant__Endpoint=http://qdrant:6333
      - Ai__EmbeddingModel__ApiKey=${EMBEDDING_API_KEY}
      - Ai__TextModel__ApiKey=${TEXT_API_KEY}
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/healthz"]
      interval: 30s
      timeout: 10s
      retries: 3
    depends_on:
      db:
        condition: service_healthy
      qdrant:
        condition: service_healthy

  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: contractclause
      POSTGRES_USER: app
      POSTGRES_PASSWORD: secret
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U app -d contractclause"]
      interval: 10s
      timeout: 5s
      retries: 5

  qdrant:
    image: qdrant/qdrant:latest
    ports:
      - "6333:6333"
    volumes:
      - qdrant_storage:/qdrant/storage
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:6333/healthz"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  pgdata:
  qdrant_storage:
```

> **注意**：`mcp-http` 服务在容器中运行 HTTP-SSE 模式。如需 stdio 模式（例如被 Agent 直接拉起），不需部署此容器，只需在 Agent 端配置进程启动命令。

### 环境变量（.env.template）

```env
EMBEDDING_API_KEY=sk-...
TEXT_API_KEY=sk-...
```

### 数据初始化

```bash
# 运行 EF Core 数据库迁移
dotnet ef database update --project src/ContractClause.Infrastructure --startup-project src/ContractClause.Api

# 运行数据导入工具（批量导入 HTML 模板）
dotnet run --project tools/ContractClause.DataImport -- --input ./data/templates/ --type 合同

# 创建初始管理员 API Key
dotnet run --project tools/ContractClause.DataImport -- --generate-api-key --owner-id <user-guid>
```

---

## 日志与可观测性

### 结构化日志（Serilog）

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.EntityFrameworkCore": "Warning",
        "System.Net.Http": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://seq:5341" }
      }
    ]
  }
}
```

### 关键埋点

| 埋点 | 级别 | 内容 |
|------|------|------|
| 模板导入 | Info | templateId, clausesCount, durationMs |
| LLM 抽取大纲 | Info | templateId, tokensUsed, success |
| 混合搜索 | Info | query, searchMode, resultCount, durationMs |
| 鉴权失败 | Warning | apiKeyPrefix, reason |
| LLM 降级 | Warning | templateId, from: llm → heuristic |
| 依赖不可达 | Error | serviceName, errorMessage |

### 健康检查端点

| 端点 | 说明 |
|------|------|
| `GET /healthz` | 存活探测（Liveness） |
| `GET /readyz` | 就绪探测（Readiness）：检查数据库 + 向量库 + AI 服务是否可达 |

---

## WorkBuddy MCP 接入指南

### 兼容性总览

| 维度 | 设计方案 | WorkBuddy 兼容性 | 说明 |
|------|----------|------------------|------|
| Transport — stdio | `command` + `args` + `env:MCP_API_KEY` | ✅ 完全兼容 | WorkBuddy 主要支持模式 |
| Transport — HTTP-SSE | `url` + `headers:X-Api-Key` | ⚠️ 未确认 | WorkBuddy 文档未明确说明 HTTP-SSE 支持，**建议优先使用 stdio** |
| 鉴权 | `MCP_API_KEY` 环境变量 | ✅ 兼容 | WorkBuddy `mcp.json` 的 `env` 字段直接传递 |
| Tool 定义 | ModelContextProtocol SDK | ✅ 兼容 | 标准 MCP 协议，无需 WorkBuddy 适配 |
| Tool 输出 | MCP `content[{type:text}]` | ✅ 兼容 | 标准 MCP 输出格式 |

### 前置条件

| 条件 | 详细说明 |
|------|----------|
| .NET Runtime | 需安装 **.NET 9.0 SDK** 或 **.NET 9.0 Runtime**（推荐 SDK，便于本地编译运行） |
| ContractClause 代码 | 克隆到本地开发机，确认 `src/ContractClause.Mcp/` 目录存在 |
| 数据库 | PostgreSQL 16+ 或 SQL Server 2019+，需提前部署或 docker-compose 启动 |
| 向量库 | Qdrant 实例，需先启动（可随 docker-compose 一起部署） |
| API Key | 需先生成一个有效的 `ApiKey`，写入 `mcp.json` 的 `env` 字段 |
| AI 模型 | 需有可用的 Embedding 和 Chat 模型端点（OpenAI 兼容接口） |

### 接入方式对比

WorkBuddy 支持两种部署拓扑，**推荐方式 A（stdio 本地模式）**：

```
方式 A：本地 stdio（推荐）
┌─────────────────────────────────────────────────────┐
│  WorkBuddy                                          │
│  ├── read mcp.json → 启动 ContractClause.Mcp 子进程   │
│  │    command: "dotnet", args: ["run", ...]          │
│  │    env: MCP_API_KEY=sk-xxx                        │
│  │                                                    │
│  └── MCP Client  ←── stdio ──→ ContractClause.Mcp    │
│                                   ↓                  │
│                              PostgreSQL / Qdrant     │
│                              (本地或远程)              │
└─────────────────────────────────────────────────────┘

方式 B：远程 HTTP-SSE（备选，需 WorkBuddy 支持）
┌──────────┐                    ┌─────────────────────┐
│ WorkBuddy │  ── HTTP SSE ──→  │ ContractClause.Mcp   │
│          │  ←── SSE 流 ───── │ (Docker / K8s 部署)   │
└──────────┘                    │     ↓               │
                                │ PostgreSQL / Qdrant  │
                                └─────────────────────┘
```

### 接入步骤（方式 A：stdio 本地模式）

#### 步骤一：部署基础设施

```bash
# 在 contractclause 项目根目录，启动数据库 + Qdrant
docker-compose up -d db qdrant

# 验证服务状态
docker-compose ps
# 预期输出：db → healthy, qdrant → healthy
```

#### 步骤二：初始化数据库和 API Key

```bash
# 1. 运行 EF Core 迁移，建表
dotnet ef database update \
  --project src/ContractClause.Infrastructure \
  --startup-project src/ContractClause.Api

# 2. 生成管理员 API Key（记录输出的 Key 值！）
dotnet run --project tools/ContractClause.DataImport \
  -- --generate-api-key --owner-type User

# 输出示例：
# ┌──────────────────────────────────────────────┐
# │ API Key: sk-a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5  │
# │ OwnerId: 550e8400-e29b-41d4-a716-446655440000 │
# │ OwnerType: User                               │
# │ 请妥善保管此 Key，配置到 WorkBuddy 的 mcp.json 中  │
# └──────────────────────────────────────────────┘
```

#### 步骤三：导入初始合同模板数据

```bash
# 将 HTML 合同模板文件放入 ./data/templates/ 目录，然后运行
dotnet run --project tools/ContractClause.DataImport \
  -- --input ./data/templates/ --type 合同

# 确认导入成功
# 观察日志输出：templateId=xxx, clausesImported=48
```

#### 步骤四：配置 appsettings（MCP 服务专用）

编辑 `src/ContractClause.Mcp/appsettings.json`（或使用环境变量覆盖）：

```json
{
  "Database": {
    "Provider": "PostgreSQL",
    "ConnectionString": "Host=localhost;Database=contractclause;Username=app;Password=secret"
  },
  "VectorStore": {
    "Provider": "Qdrant",
    "Qdrant": {
      "Endpoint": "http://localhost:6333"
    }
  },
  "Ai": {
    "EmbeddingModel": {
      "BaseUrl": "https://api.openai.com/v1",
      "ApiKey": "",
      "ModelId": "text-embedding-3-small",
      "Dimensions": 1536
    },
    "TextModel": {
      "BaseUrl": "https://api.openai.com/v1",
      "ApiKey": "",
      "ModelId": "gpt-4o-mini"
    }
  }
}
```

> **安全警告**：`ApiKey` 不要直接写在 `appsettings.json` 中！应通过**环境变量**传入：`Ai__EmbeddingModel__ApiKey=sk-xxx`。在 WorkBuddy 的 `mcp.json` 中通过 `env` 字段配置。

#### 步骤五：配置 WorkBuddy mcp.json

打开 WorkBuddy，在 **插件 → MCP 服务器 → 配置 MCP** 中编辑 `mcp.json`，添加如下配置：

```json
{
  "mcpServers": {
    "contractclause": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "/absolute/path/to/ContractClause/src/ContractClause.Mcp/ContractClause.Mcp.csproj",
        "--configuration",
        "Release"
      ],
      "env": {
        "MCP_API_KEY": "sk-a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5",
        "Ai__EmbeddingModel__ApiKey": "sk-embedding-api-key",
        "Ai__TextModel__ApiKey": "sk-text-api-key"
      }
    }
  }
}
```

**配置说明：**

| 字段 | 说明 |
|------|------|
| `command` | 固定为 `dotnet`，使用 .NET CLI 运行 |
| `args[0]` | `run`，启动项目（开发模式）。生产环境改为 `exec /path/to/ContractClause.Mcp.dll`，需先 `dotnet publish` |
| `args[2]` | 项目路径，**必须使用绝对路径**，WorkBuddy 的工作目录与项目目录可能不同 |
| `env.MCP_API_KEY` | ContractClause 的鉴权凭证，对应 `ApiKeys` 表中的 Key |
| `env.Ai__EmbeddingModel__ApiKey` | Embedding 模型的 API Key（OpenAI 或兼容接口） |
| `env.Ai__TextModel__ApiKey` | Chat 模型的 API Key |

> **生产部署优化**：开发阶段用 `dotnet run` 即可。生产环境建议先 `dotnet publish -c Release -o ./publish`，然后将 `command` 改为 `dotnet`，`args` 改为 `["exec", "/path/to/publish/ContractClause.Mcp.dll"]`，避免每次启动都编译。

#### 步骤六：验证连接

配置保存后，在 WorkBuddy 的 MCP 管理面板检查状态：

| 状态 | 说明 |
|------|------|
| 🟢 绿色 | 连接成功，MCP 服务运行正常 |
| 🔴 红色 | 连接失败，按下方排查步骤处理 |

验证成功后，可以在 WorkBuddy 中直接对话测试：

```
帮我搜索一份软件开发相关的合同模板
请获取模板 3fa85f64-... 的大纲
这份合同有哪些变量需要填写？
```

### 接入步骤（方式 B：远程 HTTP-SSE 模式）

> ⚠️ WorkBuddy 当前是否支持 HTTP-SSE 模式需以官方最新文档为准。以下是理论配置，仅作参考。

```bash
# 服务器端：部署 MCP HTTP-SSE 服务
docker-compose up -d mcp-http
```

```json
{
  "mcpServers": {
    "contractclause": {
      "url": "http://mcp.example.com:5001/mcp/sse",
      "headers": {
        "X-Api-Key": "sk-a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5"
      }
    }
  }
}
```

### 排查指南

| 问题 | 可能原因 | 解决方法 |
|------|----------|----------|
| MCP 服务状态 🔴 | API Key 无效 | 检查 `env.MCP_API_KEY` 是否正确；确认 `ApiKeys` 表中 Key 存在且 `IsDeleted=false` |
| MCP 服务状态 🔴 | 数据库不可达 | 确认 `appsettings.json` 中 `ConnectionString` 的主机地址/端口正确；`docker-compose ps` 确认 db 容器 running |
| MCP 服务状态 🔴 | Qdrant 不可达 | 确认 Qdrant 已启动；`curl http://localhost:6333/healthz` 验证 |
| MCP 服务状态 🔴 | 项目路径错误 | `args` 中项目路径必须为**绝对路径**；确认 `.csproj` 文件存在 |
| MCP 服务状态 🔴 | .NET 未安装 | 运行 `dotnet --version` 确认 .NET 9.0 SDK 已安装 |
| 搜索无结果 | 未导入模板数据 | 按步骤三执行数据导入，确认导入成功 |
| 搜索无结果 | AI 模型 API Key 无效 | 检查 `Ai__EmbeddingModel__ApiKey` 环境变量；确认模型端点可访问 |
| Tool 调用返回 401 | MCP_API_KEY 不匹配 | 重新生成 Key（步骤二），更新 `mcp.json` 中的 `env.MCP_API_KEY` |

### 针对 WorkBuddy 的设计补充建议

对 ContractClause 项目做以下调整，可更好地适配 WorkBuddy：

| 补充项 | 优先级 | 说明 |
|--------|--------|------|
| **增加 `health` Tool** | 🔴 高 | 在 MCP Tool 列表中增加一个无需鉴权（或最小鉴权）的 `health` tool，让 WorkBuddy 快速验证连接状态。返回版本号、数据库连通性、向量库连通性等信息 |
| **MCP Tool 返回结果截断** | 🔴 高 | 当前设计 `limit: max 20`。但对于 LLM context window 来说 20 条完整条款仍可能太长。增加 `compact` 参数：`true` 时只返回 id + title + summary（不含全文），让 Agent 先选定再调 `get_clauses` 获取详情 |
| **预编译发布脚本** | 🟡 中 | 在仓库根目录提供 `publish.ps1` / `publish.sh`，一键编译发布 MCP 项目为独立可执行文件，产出可直接在 WorkBuddy mcp.json 中引用的 dll 路径 |
| **Bootstrap 脚本** | 🟡 中 | 提供一键初始化脚本：`init.ps1` — 自动完成 docker-compose 启动、EF 迁移、API Key 生成、连接验证，输出可直接粘贴到 WorkBuddy mcp.json 的 JSON 片段 |
| **MCP Tool schema 自描述优化** | 🟢 低 | 为每个 Tool 的输入 schema 增加 `example` 字段，方便 Agent 在不查文档的情况下正确调用 |

---

## 风险与待办

### 关键风险

| 风险 | 等级 | 缓解措施 |
|------|------|----------|
| LLM 抽取大纲格式不稳定 | 🔴 高 | few-shot prompt + 结构化 JSON 约束 + 正则降级兜底。先做 POC 验证 |
| HTML 清洗覆盖面不足 | 🟡 中 | 收集 50+ 真实合同样本做回归测试，逐步补充清洗规则 |
| OutlineItem string ID 关联查询效率 | 🟢 低 | 模板级别查询无影响。如需按大纲项批量搜索，后续可建 OutlineItem 物理表 |

### 待办事项

- [ ] 确定 LLM 抽取大纲的 prompt 模板，用 20 份真实合同做 POC
- [ ] 收集真实合同 HTML 样本集，建立回归测试用例
- [ ] 确定 Qdrant 部署拓扑（单节点 vs 集群）
- [ ] 设计 API Key 管理界面（UI 或 CLI）
- [ ] 确定境内部署时的 AI 模型（如混元、通义千问等兼容 OpenAI 接口的国产模型）
- [ ] MCP Tool 输出增加 `compact` 模式，适配 LLM context window 长度限制
- [ ] 实现 MCP `health` Tool，供 WorkBuddy 快速验证连接状态
- [ ] 编写 `publish.ps1` 预编译脚本 + `init.ps1` 一键初始化脚本
- [ ] 确认 WorkBuddy HTTP-SSE Transport 支持情况（关注版本更新）

---

*最后更新：2026-05-26*