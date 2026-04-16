# SecondHandShopWebsite 代码审查报告

**日期:** 2026-04-16
**审查范围:** 后端 (.NET 10)、前端 (React 19)、Cloudflare Worker
**审查维度:** 安全性、性能、可维护性

---

## 总览

| 维度 | 严重 | 高 | 中 | 低 | 合计 |
|------|------|---|---|---|------|
| 安全性 | 2 | 4 | 6 | 3 | 15 |
| 性能 | 2 | 5 | 5 | 2 | 14 |
| 可维护性 | 2 | 4 | 6 | 2 | 14 |

> 去重后不同问题共计 **37 项**（部分问题跨维度重复）。

---

## 一、安全性

### CRIT-1: 真实密钥泄露到 Git 仓库 [严重]

**文件:** `src/SecondHandShop.WebApi/appsettings.Development.json`

开发配置文件已提交到 Git，包含以下真实密钥：
- Gmail 应用密码 (第 37 行)
- Cloudflare R2 AccessKeyId / SecretAccessKey / AccountId (第 42-44 行)
- remove.bg API Key (第 50 行)
- Cloudflare Turnstile SecretKey (第 53 行)

攻击者获取仓库读权限即可直接操作云存储（篡改/删除图片）、以管理员身份发送邮件、绕过验证码。

**建议:**
1. 立即轮换所有上述密钥
2. 使用 `dotnet user-secrets` 管理开发密钥，不提交到版本控制
3. 用 `git filter-repo` 或 BFG Repo Cleaner 从历史记录中清除
4. 将 `appsettings.Development.json` 加入 `.gitignore`

---

### CRIT-2: JWT 签名密钥使用已知明文默认值 [严重]

**文件:** `src/SecondHandShop.WebApi/appsettings.json:58`

```json
"Key": "REPLACE_WITH_ENV_VARIABLE_AT_LEAST_32_CHARS"
```

此字符串长 45 字符，通过了 `JwtTokenService` 的 `key.Length < 32` 检查。如果生产环境未覆盖此配置，任何知道该占位符的人都可以伪造合法管理员 JWT。

**建议:** 从 `appsettings.json` 中删除 `Jwt:Key` 字段，使未配置时启动直接报错（fail-fast），而非静默使用已知值。

---

### HIGH-1: AdminProductsController 暴露了隐藏的管理路由 [高]

**文件:** `src/SecondHandShop.WebApi/Controllers/AdminProductsController.cs:18-19`

```csharp
[Route("api/lord/products")]
[Route("api/admin/products")]   // <-- 不应存在
```

项目明确约定管理路由使用 `/api/lord/*` 而非 `/api/admin/*`。第二条路由直接暴露了可预测的管理入口。其他所有管理控制器均只有 `api/lord/*` 路由。

**建议:** 删除 `[Route("api/admin/products")]`。

---

### HIGH-2: IP 速率限制在反向代理后可被绕过 [高]

**文件:** `src/SecondHandShop.WebApi/Program.cs:158-215`

速率限制基于 `RemoteIpAddress`。ForwardedHeaders 默认 `TrustAllProxies: false`，但如果生产部署时为方便设置了 `TrustAllProxies: true`，攻击者可通过伪造 `X-Forwarded-For` 头绕过所有 IP 限制（包括登录 5次/分钟限制和询价 IP 冷却期）。

**建议:**
1. 编写生产部署文档，要求显式配置 `KnownProxies`
2. 如果 `TrustAllProxies: true`，启动时输出警告日志

---

### HIGH-3: 默认管理员凭据已提交 [高]

**文件:** `src/SecondHandShop.WebApi/appsettings.json:63-66`

```json
"AdminSeed": { "UserName": "admin", "Password": "ChangeThisInProduction" }
```

`appsettings.Development.json` 中使用 `Admin@123456`。虽然 `AdminSeedService` 设置了 `mustChangePassword: true`，但在首次部署到首次登录之间存在时间窗口，攻击者可用已知凭据登录并设置自己的密码。

**建议:** 从提交的配置文件中删除密码值，强制通过环境变量注入。`AdminSeedService` 已有空值跳过逻辑。

---

### HIGH-4: SoldAtUtc 无边界校验 [高]

**文件:** `src/SecondHandShop.WebApi/Controllers/AdminProductSalesController.cs:124-149`

管理员可以记录 `SoldAtUtc` 为 0001 年或 9999 年的销售记录，影响分析数据的准确性。

**建议:** 添加验证：`SoldAtUtc` 不超过未来 1 天，不早于业务起始日期（如 2000 年）。

---

### MED-1: 管理员密码策略偏弱 [中]

**文件:** `src/SecondHandShop.Application/Security/AdminPasswordPolicy.cs:9-10`

最低要求仅 8 字符 + 1 字母 + 1 数字。BCrypt work factor 11 可缓解离线暴力破解，但管理员账号应有更高标准。

**建议:** 最低长度提升至 12，考虑要求特殊字符。

---

### MED-2: 询价 IP 冷却检查存在 TOCTOU 竞态 [中]

**文件:** `src/SecondHandShop.Application/UseCases/Inquiries/InquiryService.cs:96-98`

`EnsureIpNotInCooldownAsync` 在事务和 advisory lock 之前执行。两个同 IP 并发请求可能同时通过此检查。

**建议:** 将冷却检查移到事务内部、`AcquireAntiSpamConcurrencyLocksAsync` 之后。

---

### MED-3: SMTP 邮件主题无 CRLF 过滤 [中]

**文件:** `src/SecondHandShop.Infrastructure/Services/SmtpEmailSender.cs:60-61, 93`

`ProductTitle` 和 `UserName` 直接插入 Subject 头。含 `\r\n` 的值可能导致 SMTP 头注入。

**建议:** 添加 sanitize 函数，移除 `\r`、`\n`、`\0`：
```csharp
static string SanitizeHeader(string? value) =>
    value?.Replace("\r", "").Replace("\n", "").Replace("\0", "") ?? string.Empty;
```

---

### MED-4: 缺少 Content-Security-Policy 头 [中]

**文件:** `src/SecondHandShop.WebApi/Program.cs:312-319`

已设置 `X-Content-Type-Options`、`X-Frame-Options` 等，但缺少 CSP。作为纯 JSON API，应添加：
```csharp
context.Response.Headers["Content-Security-Policy"] = "default-src 'none'";
```

---

### MED-5: 图片上传预签名 URL 未校验 ContentType 白名单 [中]

**文件:** `src/SecondHandShop.WebApi/Controllers/AdminProductsController.cs:149-168`

管理员可以传入任意 `ContentType`（如 `application/x-php`），生成的预签名 URL 允许上传非图片文件。

**建议:** 校验 `ContentType` 仅允许 `image/jpeg`、`image/png`、`image/webp`，同时校验文件扩展名。

---

### MED-6: Turnstile 验证未核对 hostname [中]

**文件:** `src/SecondHandShop.Infrastructure/Services/TurnstileValidator.cs:93-109`

仅检查 `success`，未验证 `hostname` 和 `action`。来自使用同一 site key 的其他网站的 token 可被重放。

**建议:** 在 `CloudflareTurnstileOptions` 中添加 `ExpectedHostname`，验证响应中的 hostname 是否匹配。

---

### LOW-1: BCrypt work factor 未显式指定 [低]

**文件:** `src/SecondHandShop.Infrastructure/Services/PasswordHasherService.cs:9`

使用库默认值 11，无法通过配置调整。

**建议:** 显式指定 `workFactor: 12`，并使其可配置。

---

### LOW-2: 基础 appsettings.json 包含默认 postgres 连接串 [低]

**文件:** `src/SecondHandShop.WebApi/appsettings.json:14`

如果生产环境未覆盖，将以 `postgres/postgres` 连接数据库。

**建议:** 清空基础配置中的连接串，缺失时让启动失败。

---

### LOW-3: Logout 端点无需认证 [低]

**文件:** `src/SecondHandShop.WebApi/Controllers/AdminAuthController.cs:72-77`

未标记 `[Authorize]`。攻击者可通过跨站 POST 清除已登录管理员的会话 cookie（`SameSite=None` 下可达）。

**建议:** 添加 `[Authorize(Policy = "AdminSession")]`。

---

## 二、性能

### CRIT-P1: 管理员客户列表每行 5 个关联子查询 [严重]

**文件:** `src/SecondHandShop.Infrastructure/Persistence/Repositories/CustomerRepository.cs:57-84`

`ListPagedForAdminAsync` 的 `.Select()` 投影中嵌入了 5 个关联子查询：
- `ProductSales.Count(...)` x2（其中一个重复计算）
- `Inquiries.Count(...)`
- `Inquiries.Where(...).Max(...)`
- `ProductSales.Where(...).Sum(...)`
- `ProductSales.Where(...).Max(...)`

每页 20 行 = 100 次子查询扫描。

**建议:** 将聚合计算提取为独立 `GROUP BY` 查询，按页内 customer ID 集合过滤后在内存中关联。

---

### CRIT-P2: OutputCache 中间件位于 Auth 之后 — 管道顺序错误 [严重]

**文件:** `src/SecondHandShop.WebApi/Program.cs:331-337`

```
UseCors → UseResponseCaching → UseRateLimiter → UseAuthentication → UseAuthorization → UseOutputCache
```

`UseOutputCache` 在认证之后，可能导致认证请求的响应被缓存后返回给未认证用户。当前 `CategoriesList`/`CategoriesTree` 是公开端点，暂无问题，但管道顺序有隐患。

**建议:** 将 `UseOutputCache()` 移到 `UseCors()` 之后、`UseAuthentication()` 之前。

---

### HIGH-P1: 询价邮件分发存在 N+1 查询 [高]

**文件:** `src/SecondHandShop.Infrastructure/Services/InquiryEmailDispatcherService.cs:71-85`

批量获取待处理询价后，循环内逐条调用 `productRepository.GetByIdAsync()` 获取产品信息。20 条询价 = 21 次数据库往返。

**建议:** 批量获取后统一查询所需产品信息（`WHERE Id IN (...)`），用字典传入分发方法。

---

### HIGH-P2: SmtpClient 每次发送都新建连接 [高]

**文件:** `src/SecondHandShop.Infrastructure/Services/SmtpEmailSender.cs:17-21, 40-44`

每封邮件都新建 TCP+TLS 连接。`System.Net.Mail.SmtpClient` 已被 Microsoft 标记为废弃。

**建议:** 迁移到 `MailKit`，使用连接池或复用单例客户端。

---

### HIGH-P3: CategoryHierarchyCache 存在缓存惊群 [高]

**文件:** `src/SecondHandShop.Infrastructure/Services/CategoryHierarchyCache.cs:15-31`

使用 `TryGetValue`/`Set` 而非 `GetOrCreateAsync`，缓存过期时多个并发请求同时打到数据库。

**建议:** 替换为 `memoryCache.GetOrCreateAsync(CacheKey, ...)`，内部自带锁机制。

---

### HIGH-P4: RemoveBgService 将完整响应体缓冲为 byte[] [高]

**文件:** `src/SecondHandShop.Infrastructure/Services/RemoveBgService.cs:94`

大图片（2-5MB+）被 `ReadAsByteArrayAsync` 加载到大对象堆（LOH），增加 GC 压力。

**建议:** 返回 `Stream`（通过 `ReadAsStreamAsync`），直接流式传输到 HTTP 响应。

---

### HIGH-P5: ListPendingEmailAsync 无批量大小限制 [高]

**文件:** `src/SecondHandShop.Infrastructure/Persistence/Repositories/InquiryRepository.cs:110-117`

返回所有待处理询价，无上限。SMTP 故障恢复后可能一次加载大量数据。

**建议:** 添加 `.Take(50)` 或 `.Take(100)` 限制，分批处理。

---

### MED-P1: 公共产品列表使用关联 EXISTS 子查询检查分类活跃状态 [中]

**文件:** `src/SecondHandShop.Infrastructure/Persistence/Repositories/ProductRepository.cs:57-58`

`.Where(p => dbContext.Categories.Any(c => c.Id == p.CategoryId && c.IsActive))` 可合并到后续已有的 JOIN 中。

**建议:** 将 `IsActive` 过滤合并到 JOIN 条件，消除额外的 EXISTS 子查询。

---

### MED-P2: AdminProductsPage filterChipSx 每次渲染创建新对象 [中]

**文件:** `frontend/src/pages/AdminProductsPage.tsx:103-113`

每次调用返回新 `sx` 对象，MUI 无法缓存样式计算。

**建议:** 将两种变体提取为模块级 `const` 对象，或使用 `styled` API。

---

### MED-P3: AnalyticsService 约 12-15 次串行数据库往返 [中]

**文件:** `src/SecondHandShop.Infrastructure/Services/Analytics/AnalyticsService.cs:36-51`

6 个顶层方法串行执行，`GetSummaryAsync` 内部又有 4-5 次独立查询。管理员端且前端有 60 秒缓存。

**建议:** 将独立子查询用 `Task.WhenAll` 并行执行（需注意 DbContext 非线程安全，可使用独立 scope 或合并 SQL）。

---

### MED-P4: SendMailAsync 未传入 CancellationToken [中]

**文件:** `src/SecondHandShop.Infrastructure/Services/SmtpEmailSender.cs:31, 53`

使用无 token 的 `SendMailAsync(MailMessage)` 重载。应用关闭时 SMTP 发送不会被取消。

**建议:** 使用 `SendMailAsync(mailMessage, cancellationToken)` 重载（.NET 6+ 可用）。

---

### MED-P5: AdminProductsPage 导航后双重 refetch [中]

**文件:** `frontend/src/pages/AdminProductsPage.tsx:194-207`

`invalidateQueries` 之后又调用 `productsQuery.refetch()`，导致两次并发请求。

**建议:** 移除 `productsQuery.refetch()`，`invalidateQueries` 已足够触发重新获取。同时从 `useEffect` 依赖数组中移除 `productsQuery`。

---

### LOW-P1: 前端 Query Key 使用整个 params 对象 [低]

**文件:** `frontend/src/pages/ProductsPage.tsx:35`

当前 `useMemo` 保证了引用稳定，但如果未来重构移除 `useMemo` 会导致每次渲染重新请求。

**建议:** 将字段展开到 query key 中：`['products-paged', params.page, params.category, ...]`。

---

### LOW-P2: TimeZoneInfo 每次登录通知都重新查找 [低]

**文件:** `src/SecondHandShop.Infrastructure/Services/SmtpEmailSender.cs:177-184`

**建议:** 缓存为 `private static readonly TimeZoneInfo _nzTimeZone = ResolveNewZealandTimeZone()`。

---

## 三、可维护性

### CRIT-M1: BumpTokenVersion 方法不存在但被文档引用 [严重]

**文件:** `src/SecondHandShop.Domain/Entities/AdminUser.cs:25`

XML 注释引用 `<see cref="BumpTokenVersion"/>`，但该方法不存在。`TokenVersion` 仅在 `CompleteForcedPasswordChange` 中递增。这意味着：
- 管理员被 `SetActive(false)` 停用后，虽然 JWT 验证会检查 `IsActive`，但如果被重新激活，旧 JWT 又会变为有效
- 缺少通用的 token 失效手段

**建议:** 添加 `public void BumpTokenVersion() => TokenVersion++` 方法，在 `SetActive(false)` 中调用。

---

### CRIT-M2: 零测试覆盖 [严重]

前端无任何 `.test.ts`/`.test.tsx`/`.spec.ts` 文件。后端无 `*.Tests` 项目。一个处理金融交易（销售记录）、客户数据、反垃圾策略的市场应用完全没有自动化测试。

**建议（按优先级）:**
1. 后端：为领域实体状态机（`Product.MarkAsSold`、`Product.RevertSoldToAvailable`）添加单元测试
2. 后端：为 `InquiryService.CreateInquiryAsync` 反垃圾逻辑添加集成测试
3. 后端：为 `LoginAdminCommandHandler` 锁定逻辑添加测试
4. 前端：为 `ProductSaleDialog` 表单验证添加组件测试

---

### HIGH-M1: useEffect 依赖数组包含不稳定引用 [高]

**文件:** `frontend/src/pages/AdminProductsPage.tsx:194-207`

`productsQuery` 每次渲染都生成新引用，放入 `useEffect` 依赖数组可能导致无限循环（React Strict Mode 或 query 状态变化时）。

**建议:** 移除 `productsQuery` 依赖和 `refetch()` 调用，仅使用 `queryClient.invalidateQueries`。

---

### HIGH-M2: SmtpClient 已废弃且不支持异步取消 [高]

**文件:** `src/SecondHandShop.Infrastructure/Services/SmtpEmailSender.cs`

Microsoft 明确建议使用 MailKit 替代。当前实现每次新建实例，不支持取消，应用关闭时可能阻塞。

**建议:** 迁移到 `MailKit.Net.Smtp.SmtpClient`。

---

### HIGH-M3: GetAdminUserId() 返回 null 时静默继续 [高]

**文件:** `src/SecondHandShop.WebApi/Controllers/AdminProductsController.cs:201-206`，`AdminProductSalesController.cs:116-121`

如果 JWT 缺少有效的 `sub` claim，`GetAdminUserId()` 返回 `null`，该值被传入领域方法记录到审计字段，导致"创建者为空"的无法追溯记录。

**建议:** 返回 null 时立即返回 `Unauthorized()` 或记录错误日志。

---

### HIGH-M4: AdminSeedService 使用同步 using 而非 await using [高]

**文件:** `src/SecondHandShop.Infrastructure/Services/AdminSeedService.cs:16`

```csharp
using var scope = serviceProvider.CreateScope(); // 应使用 CreateAsyncScope()
```

其他后台服务均使用 `await using var scope = scopeFactory.CreateAsyncScope()`。同步 `Dispose` 可能无法正确清理异步 `DbContext`。

**建议:** 改为 `await using var scope = serviceProvider.CreateAsyncScope()`。

---

### MED-M1: 请求 Record 定义在控制器文件内 [中]

**文件:** `AdminProductsController.cs:219-273`，`AdminProductSalesController.cs:124-158`，`AdminCustomersController.cs:158-199`

API 请求/响应类型混在控制器末尾，不易发现和复用。`Application/Contracts/` 已建立了 DTO 分层约定。

**建议:** 将 API 层请求 Record 移到 `WebApi/Contracts/` 文件夹。

---

### MED-M2: InquiryEmailDispatcherService.DispatchOneAsync 硬编码 CancellationToken.None [中]

**文件:** `src/SecondHandShop.Infrastructure/Services/InquiryEmailDispatcherService.cs:85, 108`

忽略了外部传入的 `stoppingToken`，应用关闭时数据库查询和邮件发送不会被取消。

**建议:** 为 `DispatchOneAsync` 添加 `CancellationToken` 参数，传递 `stoppingToken`。

---

### MED-M3: CategoryHierarchyCache.Invalidate() 从未被调用 [中]

**文件:** `src/SecondHandShop.Infrastructure/Services/CategoryHierarchyCache.cs`

`Invalidate()` 方法存在但全代码库无调用点。分类创建/停用后缓存可能在 5 分钟 TTL 内提供过期数据。

**建议:** 在 `CreateCategoryCommandHandler` 中调用 `Invalidate()`，或删除此方法并在注释中说明依赖 TTL 过期。

---

### MED-M4: resolveErrorMessage 重复实现 [中]

**文件:**
- `frontend/src/pages/AdminProductsPage.tsx:91-100`
- `frontend/src/features/admin/components/ProductSaleDialog.tsx:49-53`

两处用不同方式从 Axios 错误中提取消息。

**建议:** 提取为 `shared/utils/errorMessage.ts` 统一导入。

---

### MED-M5: ESLint 未强制 no-explicit-any [中]

**文件:** `frontend/eslint.config.js`

`@typescript-eslint/no-explicit-any` 仅为 warning 级别，无无障碍访问 lint 插件。

**建议:** 升级为 `"error"` 级别，添加 `eslint-plugin-jsx-a11y`。

---

### MED-M6: MediatR 与直接服务调用混用缺乏文档约定 [中]

部分操作（登录、session 刷新）使用 MediatR handler，部分（目录管理、销售）使用直接服务接口。功能上可维护但缺少设计决策文档。

**建议:** 在 CLAUDE.md 或 Architecture Decision Record 中记录该约定及其原因。

---

### LOW-M1: AdminProductListItem 缺少 condition 字段 [低]

**文件:** `frontend/src/features/admin/api/adminApi.ts`

后端返回 `Condition` 字段（字符串），前端 TypeScript 接口未定义此字段，数据被静默丢弃。

**建议:** 添加 `condition?: string` 到 `AdminProductListItem` 接口。

---

### LOW-M2: SmtpEmailSender 硬编码新西兰时区 [低]

**文件:** `src/SecondHandShop.Infrastructure/Services/SmtpEmailSender.cs:177-187`

`"Pacific/Auckland"` 硬编码在基础设施层。

**建议:** 低优先级（业务在新西兰），将时区 ID 移到 `SmtpEmailOptions` 配置中。

---

## 四、修复优先级建议

### 立即处理（1-2 天）

| 编号 | 问题 | 预估工作量 |
|------|------|-----------|
| CRIT-1 | 轮换泄露密钥 + 清理 Git 历史 | 2h |
| CRIT-2 | 移除默认 JWT Key | 15min |
| HIGH-1 | 删除 `api/admin/products` 路由 | 5min |
| HIGH-3 | 移除提交的管理员默认密码 | 15min |
| LOW-2 | 清空基础 appsettings 连接串 | 5min |
| LOW-3 | Logout 端点加认证 | 5min |

### 短期完成（1-2 周）

| 编号 | 问题 | 预估工作量 |
|------|------|-----------|
| CRIT-M1 | 添加 BumpTokenVersion 方法 | 1h |
| CRIT-P1 | 重构客户列表查询 | 3h |
| CRIT-P2 | 调整 OutputCache 中间件顺序 | 30min |
| HIGH-P1 | 修复询价邮件 N+1 查询 | 2h |
| HIGH-P3 | CategoryHierarchyCache 使用 GetOrCreateAsync | 30min |
| HIGH-P5 | 添加 ListPendingEmailAsync 批量限制 | 30min |
| HIGH-M3 | GetAdminUserId null 检查 | 30min |
| HIGH-M4 | AdminSeedService 改 async scope | 15min |
| MED-2 | IP 冷却检查移入事务 | 1h |
| MED-3 | SMTP Subject 过滤 CRLF | 30min |
| MED-5 | 图片上传 ContentType 白名单 | 1h |

### 中期规划（1-2 月）

| 编号 | 问题 | 预估工作量 |
|------|------|-----------|
| CRIT-M2 | 建立测试框架 + 核心逻辑单元测试 | 2-3 周 |
| HIGH-P2/HIGH-M2 | 迁移到 MailKit | 1 天 |
| HIGH-P4 | RemoveBgService 流式返回 | 2h |
| MED-P3 | Analytics 查询并行化 | 3h |
| MED-1 | 管理员密码策略强化 | 1h |
| MED-6 | Turnstile hostname 校验 | 1h |

---

## 五、架构健康度总评

**合规项（做得好的地方）:**

- Clean Architecture 层级边界正确：Domain → Application → Infrastructure/WebApi 依赖方向无违规
- DI 生命周期基本正确：Singleton/Scoped 用法合理
- 全局异常处理 `ApiExceptionFilter` 覆盖预期异常类型
- 结构化日志使用 Serilog，敏感数据（密码/token）未出现在日志中
- 前端使用 React Query 管理服务器状态，无 Redux 冗余
- 前端功能切片架构清晰：entities → features → pages
- Admin 认证采用 HttpOnly Cookie + JWT，安全基础良好
- 使用 advisory lock 防范询价并发竞态（虽有小瑕疵）

**需要关注的系统性问题:**

1. **密钥管理** — 缺乏统一的密钥管理方案，多处真实密钥提交到版本控制
2. **测试缺失** — 最大的可维护性风险，任何重构无法自动验证正确性
3. **数据库查询** — 多处关联子查询和 N+1 问题，需系统性优化
4. **邮件基础设施** — `System.Net.Mail.SmtpClient` 已废弃，需迁移到 MailKit
