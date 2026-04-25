# SecondHandShop 测试优化建议与执行计划

日期：2026-04-20
依据：`testing-review-2026-04-20`（本次审查结论，评级 B-）
作者：代码审查产出
状态：待执行

---

## 0. 文档定位

本文件不是一次性补丁清单，而是一份**分阶段、可分派**的执行计划：
每一条任务都给出「目标」「前置条件」「具体改动点」「验收标准」「预估工作量」，便于按迭代拆分、交付与跟踪。

已有文档 `docs/testing-strategy.md`、`docs/testing-gap-report-2026-04-17.md`、`docs/testing-execution-plan-2026-04-17.md` 为背景，本计划聚焦 2026-04-20 审查中新发现或仍未闭合的问题。

工作量标尺：
- **S**：≤ 0.5 天
- **M**：0.5–1.5 天
- **L**：1.5–3 天
- **XL**：3 天以上，或需设计评审

---

## 1. 总体路线图

| 阶段 | 时间窗口（建议） | 主题 | 交付物 |
|---|---|---|---|
| **Phase 1** | Week 1 | 统一基础设施 + 安全相关 P0 补齐 | 公共 `FakeClock`、Docker skip 策略、Turnstile/限流/安全头测试 |
| **Phase 2** | Week 2 | 领域层和基础设施服务空白补齐 | 7 个实体单测、5 个关键 Infra 服务单测 |
| **Phase 3** | Week 3 | Web API 真实端到端 smoke + 遗漏控制器 | 真实 PG + Handler 链路冒烟、`ImageProcessing`/`AdminPing` 测试 |
| **Phase 4** | Week 4 | 前端公共页面 + shared 层 | `ProductsPage`/`ProductDetailPage` 组件测试、`httpClient`/`adminAuth`/`imageUrl` 测试 |
| **Phase 5** | Week 5 | E2E 分层改造 + 覆盖率门禁 | MSW 层 + smoke 层、Coverlet 报告、CI 阈值 |

每个阶段结束应满足：`dotnet test` + `npm run test` + `npm run test:e2e` 全绿，且 CI 报告更新。

---

## 2. Phase 1 — 基础设施与 P0 安全补齐

### 2.1 统一 `IClock` 测试替身 [S]

**目标**：消除各处 `StubClock`、内联 `DateTime.UtcNow` 重复，统一时间伪造入口。

**前置条件**：无。

**具体改动点**：
1. 新增 `tests/SecondHandShop.TestCommon/Time/FakeClock.cs`
   ```csharp
   public sealed class FakeClock : IClock
   {
       public DateTime UtcNow { get; set; }
       public FakeClock(DateTime utcNow) => UtcNow = utcNow;
       public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
   }
   ```
2. 新建共享测试支持工程 `SecondHandShop.TestCommon`（`<IsPackable>false</IsPackable>`），其他 4 个测试工程引用。
3. 替换 `InquiryServiceTests.StubClock` 等内联实现。

**验收标准**：
- 所有单测不再直接使用 `DateTime.UtcNow`（允许在 Infrastructure 集成测试中保留，因为那里跑 SQL 聚合）。
- `grep "StubClock\|private sealed class.*IClock" tests/` 无命中。

### 2.2 统一 Docker/数据库 skip 策略 [S]

**问题**：`PostgresFixture.SkipIfUnavailable` 用 `Assert.Fail`，E2E 用 `test.skip`，语义不一致。

**改动点**：
1. Infrastructure 测试引入 `SkippableFact`（`Xunit.SkippableFact` 包），`PostgresFixture.SkipIfUnavailable` 在无 Docker 时调用 `Skip.If(...)`。
2. CI 流水线中 Docker 必须可用 —— 添加 `docker info` 前置检查，缺失时流水线硬失败，而不是让测试静默跳过。
3. 本地开发者模式保留 skip，方便无 Docker 时快速跑其他单测。
4. 环境变量 `REQUIRE_DOCKER=true` 时，强制失败而非 skip（CI 使用）。

**验收标准**：
- 在本地 `docker stop` 后运行集成测试，显示为 skipped 而非 failed。
- CI 任一集成测试 skipped 视为失败（通过 xUnit 的 `--minimum-expected-tests` 或在脚本中断言）。

### 2.3 `TurnstileValidator` 单测 [M]

**目标**：覆盖外部 HTTP 调用分支，避免线上 Turnstile 异常被吞。

**改动点**：新增 `tests/SecondHandShop.Infrastructure.UnitTests/Services/TurnstileValidatorTests.cs`：
1. 使用 `Moq` 的 `HttpMessageHandler` 或 `RichardSzalay.MockHttp` 伪造 `siteverify` 响应。
2. 用例（至少 6 条）：
   - 成功 → `IsSuccess = true`
   - 失败且 `error-codes: ["invalid-input-response"]` → `IsSuccess = false` 且携带错误码
   - 网络超时（`TaskCanceledException`）→ 抛 `TurnstileValidationUnavailableException`
   - HTTP 5xx → 抛 `TurnstileValidationUnavailableException`
   - 解析失败（空体/非 JSON）→ 抛 `TurnstileValidationUnavailableException`
   - `SecretKey` 为空时的早期失败

**验收标准**：Line coverage ≥ 90%，包含 catch 分支。

### 2.4 限流与安全头契约测试 [M]
**目标**：验证 `/api/lord/auth/login`（5/min）与 `/api/products/search`（30/min）的限流语义，以及安全头存在性。

**改动点**：在 `WebApi.IntegrationTests` 新增 `Controllers/RateLimitingTests.cs` 与 `Middleware/SecurityHeadersTests.cs`：
1. **限流**：循环 POST 登录 6 次，第 6 次应为 `429 Too Many Requests`；等待窗口后恢复。对搜索端点做同样的 31 次验证。
2. **安全头**：单次 GET `/api/categories` 断言 `X-Content-Type-Options: nosniff`、`X-Frame-Options: DENY`、`Referrer-Policy`、`Permissions-Policy` 的存在。
3. **CorrelationId**：请求无头时响应应包含自动生成的 `X-Correlation-Id`；请求带头时回显同值。

**注意事项**：
- `TestWebApplicationFactory` 目前禁用了 hosted services，但未禁用 `RateLimiter`；确认中间件管线在测试环境运行。
- 必要时使用 `TestServer.CreateClient` 并复用 `WebApiIntegrationCollection`。

**验收标准**：限流测试在 2 秒内稳定完成；安全头 4 项断言全通过。

### 2.5 JWT 授权策略补全 [S]

**目标**：覆盖 `AdminSession` 与 `AdminFullAccess` 在敏感端点的差异。

**改动点**：扩展 `AdminAuthAndAuthorizationTests.cs`：
- `PUT /api/lord/auth/change-initial-password` 在 `PasswordChangeRequired=true` 时应返回 200/204（`AdminSession` 允许）。
- `GET /api/lord/customers` 在 `PasswordChangeRequired=true` 时应返回 403（`AdminFullAccess` 拒绝）。
- `tokenVersion` 不匹配时 401（需 `IAdminUserRepository.GetByIdAsync` 返回更高 version）。

**验收标准**：新增 3 条断言，现有测试无 regression。

---

## 3. Phase 2 — 领域与基础设施空白

### 3.1 领域实体单测补齐 [M]

逐实体最小用例集（每实体 2–4 条）：

| 实体 | 必测行为 |
|---|---|
| `AdminUser` | 创建、改密（`MarkPasswordChanged`）、`BumpTokenVersion`、`RequiresInitialPasswordChange` 初值 |
| `Category` | 创建、`Activate/Deactivate`、层级路径更新、`Rename` 变更 slug 影响 |
| `Inquiry` | 创建、`MarkEmailDelivered`、`MarkEmailFailed`（状态机） |
| `InquiryIpCooldown` | `IsActive(now)` 边界、`Extend` 的时间累加 |
| `ProductCategory` | 主分类唯一性（只能 1 条 `IsMain=true`） |
| `ProductImage` | `UpdateSortOrder`、`MarkAsPrimary` 互斥性 |
| `ProductSale` | `Cancel` 合法状态、`MarkDelivered`、拒绝重复取消 |

**同步补**：
- `SlugValidator` 用 `[Theory]` + 多组数据：空串、含空格、含中文、长度上限（例如 120 字符）。
- `EmailAddressSyntaxValidator`：RFC 常见合法/非法样本。

**验收标准**：Domain 项目 line coverage ≥ 80%。

### 3.2 Infrastructure 关键服务单测 [L]

优先级排序：

1. **`JwtTokenService`**（S）：生成、解析、签名校验、版本声明；在边界时钟下过期判断。
2. **`PasswordHasherService`**（S）：BCrypt round-trip、`NeedsRehash` 当工作因子变化。
3. **`CategoryHierarchyCache`**（M）：并发 `GetOrLoadAsync`（验证只回源一次）、`Invalidate` 后再次回源、缓存过期行为。
4. **`AdminSeedService`**（M）：空库时种子创建、已有管理员时跳过、`ForcePasswordChange` 配置生效。
5. **`CatalogSeedService`**（M）：覆盖幂等（重复启动不重复写入）。
6. *（可延后）* `R2ObjectStorageService`、`RemoveBgService`、`SmtpEmailSender`、`InquiryEmailDispatcherService` — 依赖外部 SDK，建议用契约级 fake，作为 Phase 3 一部分。

**执行方式**：新建 `tests/SecondHandShop.Infrastructure.UnitTests` 项目（与 Integration 分离），纯 Moq，无 Docker 依赖。

**验收标准**：每个服务 happy path + 至少 2 条错误分支；整体 Infrastructure 项目 line coverage ≥ 60%。

### 3.3 `AnalyticsService` 补边界用例 [S]

现有仅 "空库" + "30 天聚合"。补：
- 跨时区边界（`AnalyticsDateRange.Last7Days` 与本地时区无关）。
- 有取消销售（`SaleRecordStatus.Cancelled`）时不计入收入。
- 热度榜去重（同一产品多 inquiry 合并计数）。

**Current status (2026-04-25)**: Completed and verified.
- Added boundary coverage for cancelled sales, UTC Last7Days windows, all-time ranges, and hot-unsold demand.
- Analytics integration tests now reset analytics data per test so the shared PostgreSQL fixture remains deterministic.

---

## 4. Phase 3 — Web API 端到端冒烟与遗漏控制器

### 4.1 真实管线端到端冒烟 [L]

**问题**：当前 WebApi 测试全部 Mock `IMediator`/Services/Repositories，名为集成实为契约。

**改动点**：新增 `tests/SecondHandShop.WebApi.E2ETests`（或在现有项目里加 `RealStack/` 目录）：
1. 复用 `PostgresFixture`（跨工程共享 fixture，或测试项目各自启动一份容器）。
2. `TestWebApplicationFactory.RealStack` 派生类：**只**替换外部依赖（Turnstile、remove.bg、R2、SMTP）为 fake，其余保留真实实现。
3. 冒烟用例（5–8 条即可）：
   - 管理员登录 → 创建分类 → 创建商品 → 首页 `/api/products/search` 看到
   - 公共端提交 `Inquiry` → 数据库落库 → 出站 fake 邮件队列收到消息
   - 管理员 `MarkAsSold` → 分析接口 30 天窗口汇总数字 +1
   - 管理员改密 → 新 token 可通过 `AdminFullAccess`、旧 token 失效

**验收标准**：冒烟套件在本地 < 30s，CI < 60s。

**Current status (2026-04-25)**: Added real-pipeline smoke coverage under
`tests/SecondHandShop.WebApi.IntegrationTests/RealStack/`:
- Admin login -> create category -> create product -> product is visible in public search.
- Public Inquiry submission -> database persistence -> fake SMTP receives via the real hosted dispatcher.
- MarkAsSold -> 30-day Analytics summary includes the sold item and revenue.
- Forced password change -> restricted token lacks `AdminFullAccess`, old token is revoked, new login token works.

`RealStackWebApplicationFactory` only replaces Turnstile, R2, remove.bg, and SMTP. MediatR,
repositories, EF, JWT/password hashing, and hosted dispatchers remain real. The suite skips when
local Docker/Postgres is unavailable; CI can set `REQUIRE_DOCKER=true` to fail hard instead.
Resume this session with:
claude --resume "test-optimization-execution-plan"

### 4.2 遗漏控制器 [S]

- `AdminPingController`：1 条 — 带合法 cookie 返回 200、无 cookie 返回 401。
- `ImageProcessingController`：4 条 — 权限（未登录 401）、不支持的 MIME 400、remove.bg 失败 502、成功返回上传 URL。

### 4.3 并发/`RowVersion` 场景 [M]

- `Product.UpdateDetails` 场景：两个并发请求基于同一 `RowVersion` 更新，后一个期望抛 `DbUpdateConcurrencyException` → 映射为 409。
- 需在 `ProductRepository` 集成测试里模拟（两个 DbContext 同步保存）。

**Current status (2026-04-25)**: Completed in
`tests/SecondHandShop.Infrastructure.IntegrationTests/Repositories/ProductConcurrencyTests.cs`.

---

## 5. Phase 4 — 前端补齐

### 5.1 共享层测试 [M]

优先于页面，因为这些是所有页面都依赖的基础设施。

| 模块 | 用例 |
|---|---|
| `shared/api/httpClient.ts` | 401 命中管理路径时跳转 `/lord/login`；401 命中公共路径不跳转；非 401 透传错误；超时重试策略（如有） |
| `features/admin/auth/adminAuth.ts` | `initializeAdminAuth` 轮询、`persistSessionAfterLogin` 写入过期、`revokeLordCookie` 调 logout 端点、`useAdminAuth` 订阅 snapshot |
| `shared/utils/imageUrl.ts` | `null`、相对 key、绝对 URL 三类输入输出；空 `VITE_IMAGE_BASE_URL` 的退化 |

**实施**：新增 `frontend/src/shared/**/__tests__/*.test.ts`；401 跳转使用 `window.location.assign` 的 spy（或注入跳转钩子以便可测试）。

### 5.2 公共页面组件测试 [M]

| 页面 | 关键断言 |
|---|---|
| `HomePage` | 精选商品加载、空态、错误态 |
| `ProductsPage` | 搜索关键字提交、分页切换、价格区间筛选、分类选择同步到 URL |
| `ProductDetailPage` | 轮播图切换、下架商品不显示询价入口、`Send Inquiry` 链接 href |
| `NotFoundPage` | 返回首页链接可点击 |

所有外部调用通过 `vi.mock('../../features/.../api/...')` 隔离；路由通过 `MemoryRouter` + `initialEntries`。

### 5.3 `AdminNewProductPage` 图像流 [M]

分步测试（难点是上传 → remove.bg → R2 presign 的多步）：
1. 表单必填校验
2. Presigned URL 请求失败时的错误呈现
3. 移除背景被拒（API key 无效）时仍允许跳过
4. 成功流程调用 `addProductImage` 的参数

**Current status (2026-04-25)**: Phase 4 frontend coverage is in place.
- Shared tests cover admin 401 handling in `httpClient`, auth bootstrap/session helpers, and image URL fallback/relative/absolute behavior.
- Public page tests cover Home featured products, Products URL filters/category/pagination/empty/error states, ProductDetail image switching/inquiry availability, and NotFound home navigation.
- `AdminNewProductPage` tests cover required validation, presigned upload failure, remove.bg failure with original-image continuation, and cutout upload metadata.
- A small behavior fix now keeps sold product detail pages from rendering an inquiry link.

---

## 6. Phase 5 — E2E 分层与覆盖率门禁

### 6.1 E2E 双层拆分 [L]

**现状问题**：单一 spec，大量 `test.skip` 导致 CI 可能全绿但无有效断言。

**目标结构**：
```
frontend/
├── tests/
│   ├── journey/              # 新增：MSW 模拟后端的 UI 流程
│   │   ├── admin-login.journey.ts
│   │   ├── public-inquiry.journey.ts
│   │   └── server.ts         # MSW handlers
│   └── e2e/                  # 只保留真·真实后端冒烟
│       ├── admin-login.smoke.spec.ts
│       └── public-catalog.smoke.spec.ts
```

**改动点**：
1. 安装 `msw` + 为每个 journey 定义 handlers。
2. `journey` 套件在 CI 默认跑，不依赖后端。
3. `e2e` smoke 只在 Nightly 或 pre-release 跑，环境缺失时**直接失败**（去掉静默 skip）。
4. Playwright project 拆分为 `journey` 与 `smoke` 两个 project。

### 6.2 覆盖率采集与门禁 [M]

**后端**：
1. 在每个测试工程引入 `coverlet.collector`（已有 SDK 可能自动引入，确认版本 ≥ 6.0）。
2. 添加 `scripts/coverage.ps1` / `coverage.sh`：
   ```bash
   dotnet test --collect:"XPlat Code Coverage" --results-directory .coverage
   reportgenerator -reports:.coverage/**/coverage.cobertura.xml -targetdir:.coverage/report
   ```
3. 设置阈值（起步值，后续逐步上调）：
   | 层 | 首期门禁 | 目标 |
   |---|---|---|
   | Domain | 70% | 85% |
   | Application | 65% | 80% |
   | Infrastructure | 45% | 65% |
   | WebApi | 55% | 70% |

**前端**：
1. `vitest.config.ts` 启用 `coverage: { provider: 'v8', reporter: ['text', 'html', 'cobertura'] }`。
2. 初期门禁：`lines: 50, branches: 45, functions: 50, statements: 50`。
3. 在 CI 失败日志中列出未覆盖的关键文件清单（`shared/`、`features/*/api/`）。

### 6.3 CI 流水线更新 [S]

- GitHub Actions / Azure Pipelines 中新增 3 个 job：
  - `backend-unit`（不要 Docker）
  - `backend-integration`（要 Docker，强制 `REQUIRE_DOCKER=true`）
  - `frontend-unit` + `frontend-journey`
  - `nightly-e2e`（独立触发）
- PR 合并前必须 `backend-unit` + `backend-integration` + `frontend-unit` + `frontend-journey` 全绿。

**Current status (2026-04-25)**: Phase 5 groundwork is implemented.
- Playwright is split into `journey` and `smoke` projects. Journey tests mock API routes in the browser and run without a backend; smoke tests target the real backend and fail fast when required environment/backing services are unavailable.
- Added `frontend/tests/journey/admin-login.journey.ts`, `frontend/tests/journey/public-inquiry.journey.ts`, and shared route handlers in `frontend/tests/journey/server.ts`.
- Replaced the old all-in-one E2E spec with `admin-login.smoke.spec.ts` and `public-catalog.smoke.spec.ts`.
- Added frontend coverage config and `npm run test:coverage` using V8 coverage with initial thresholds.
- Added backend coverage scripts: `scripts/coverage.ps1` and `scripts/coverage.sh`, using `coverlet.collector` plus `reportgenerator`.
- CI now separates `backend-unit`, `backend-integration`, `frontend-unit`, `frontend-journey`, and scheduled/manual `nightly-e2e`.

**Current status (2026-04-25 update)**: Coverage gates and missing Phase 2 service tests are completed.
- Added `CategoryHierarchyCache`, `AdminSeedService`, and `CatalogSeedService` unit tests.
- Added focused frontend API/helper tests for public catalog/home/inquiry adapters, admin analytics helpers, and admin API wrappers.
- Backend coverage thresholds are enforced by `scripts/check-coverage-thresholds.ps1` and `scripts/check-coverage-thresholds.sh`.
- Latest measured backend line coverage: Domain 88.8%, Application 83.6%, Infrastructure 92.2%, WebApi 73.8%.
- Latest measured frontend line coverage: 67.7%, with `shared/` and key `features/*/api/` modules directly covered.
- Added `.github/pull_request_template.md` with the testing review checklist.

---

## 7. 跨阶段的工程化约定

### 7.1 测试代码规范
- **命名**：`Method_ShouldDoX_WhenY` / `Should_DoX_When_Y`（前端 `it('does X when Y')`）保持统一。
- **禁用**：单测不得命中 `DateTime.UtcNow`、`Guid.NewGuid()` 以外的隐式非确定源；必要随机数使用 `Random(seed)`。
- **尺寸**：单测文件 ≤ 250 行；超过时拆 `XxxHappyPathTests` / `XxxErrorPathTests`。
- **Builder 模式**：多处重复的 `Product.Create(...)` 等抽到 `tests/SecondHandShop.TestCommon/Builders/ProductBuilder.cs`。

### 7.2 评审检查表（加入 PR 模板）
```
- [ ] 新增业务逻辑是否有测试？
- [ ] 异常/边界路径是否被覆盖？
- [ ] 是否引入了对 DateTime.UtcNow 的直接依赖？
- [ ] 集成测试是否更新了必要的 SeedHelper？
- [ ] 覆盖率是否下降（对比 main 分支报告）？
```

### 7.3 文档与追踪
- 每个阶段完成后更新 `docs/testing-strategy.md` 中的「已实施状态」小节。
- 在 `docs/session-handoff/` 为每个阶段结束写一份短 handoff（任务 / 未决项 / 下一步）。

---

## 8. 任务清单一览（可直接拆为 issue）

| # | 标题 | 阶段 | 工作量 | 依赖 |
|---:|---|:-:|:-:|---|
| T01 | 抽取 `SecondHandShop.TestCommon` 工程与 `FakeClock` | 1 | S | — |
| T02 | Docker skip 统一策略（Skippable + REQUIRE_DOCKER） | 1 | S | — |
| T03 | `TurnstileValidator` 单测 | 1 | M | — |
| T04 | 限流 + 安全头 + CorrelationId 契约测试 | 1 | M | — |
| T05 | JWT 授权策略补全（3 条） | 1 | S | — |
| T06 | Domain 实体 7 个补测 | 2 | M | T01 |
| T07 | `JwtTokenService` / `PasswordHasherService` 单测 | 2 | S | — |
| T08 | `CategoryHierarchyCache` 并发单测 | 2 | M | — |
| T09 | `AdminSeedService` / `CatalogSeedService` 幂等单测 | 2 | M | — |
| T10 | `AnalyticsService` 边界场景 | 2 | S | — |
| T11 | WebApi `E2ETests` 项目 + 真实栈冒烟 5–8 条 | 3 | L | T01 |
| T12 | `AdminPing` / `ImageProcessing` 控制器测试 | 3 | S | — |
| T13 | `Product` 并发/`RowVersion` 测试 | 3 | M | — |
| T14 | 前端 `httpClient` / `adminAuth` / `imageUrl` 测试 | 4 | M | — |
| T15 | 前端公共页面 4 个测试 | 4 | M | T14 |
| T16 | `AdminNewProductPage` 图像流测试 | 4 | M | — |
| T17 | E2E 拆分为 journey (MSW) + smoke 两层 | 5 | L | — |
| T18 | Coverlet + 前端 v8 覆盖率采集脚本 | 5 | M | — |
| T19 | CI 更新：4 个必选 job + Nightly E2E | 5 | S | T17, T18 |
| T20 | 覆盖率门禁首期阈值 | 5 | S | T18 |

**估算总量**：≈ 18–22 人日，建议两位开发者分头推进约 2–3 周完成 Phase 1–4，Phase 5 单独 0.5–1 周收尾。

---

## 9. 风险与回退

| 风险 | 影响 | 缓解 |
|---|---|---|
| 真实栈冒烟引入数据库启动时间 | CI 变慢 | 复用 `PostgresFixture` 单容器；与 Integration 套件合并 job |
| 覆盖率门禁过紧阻塞 PR | 开发体验下降 | 首期阈值保守，每迭代上调 5% |
| MSW 与真实后端契约漂移 | 前端通过但实际破 | 新增 contract 测试：Playwright journey 抽样对真实 OpenAPI schema 校验 |
| `SkippableFact` 引入新依赖 | 包管理负担 | 版本锁定，仅集成测试工程引用 |
| Turnstile/remove.bg fake 与真实 API 差异 | 漏检线上故障 | Nightly smoke 打真实沙箱密钥 |

---

## 10. 完成定义（DoD）

整个计划达成以下状态视为闭合：

1. 4 个后端测试工程 + 2 个新增工程（`TestCommon`、`Infrastructure.UnitTests`、可选 `WebApi.E2ETests`）全部参与 CI。
2. 后端覆盖率：Domain ≥ 85%、Application ≥ 80%、Infrastructure ≥ 65%、WebApi ≥ 70%。
3. 前端覆盖率 lines ≥ 60%，关键 `shared/` 与 `features/*/api/` 目录 100% 被测。
4. E2E 双层到位，journey 套件 PR 门禁，smoke 套件 Nightly。
5. Docker skip 策略在本地与 CI 行为明确可预测。
6. 所有 20 条任务关闭，关联 PR 合并入 main。
7. `docs/testing-strategy.md` 的现状章节与实际一致。

**Current status (2026-04-25 update)**: Implementation work for T01-T20 is complete in the working tree.
Final closure still requires review and merge into `main`.
