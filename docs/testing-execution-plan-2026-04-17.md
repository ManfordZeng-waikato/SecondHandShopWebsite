# SecondHandShop 测试补齐执行计划

日期：2026-04-17

## 目标

将当前“局部回归网”扩展为可持续维护的业务测试体系，优先保护真实业务入口、后台核心流程和高风险聚合查询。

本计划遵循两个原则：

- 优先补真实业务入口，而不是先追求测试数量
- 优先补回归风险高、变更频率高、业务损失大的流程

## 执行策略

执行顺序固定为：

1. Web API 集成测试
2. Application 单元测试
3. Infrastructure 集成测试
4. 前端组件测试
5. E2E 闭环测试

原因：

- API 层最接近真实业务入口，最能防止“代码能跑但接口坏了”
- Application 层负责业务编排，适合补分支和错误路径
- Infrastructure 层负责复杂查询和持久化约束，适合补数据库行为
- 前端和 E2E 留到后面补，避免在后端契约尚未稳定时增加维护成本

## 总体验收标准

完成本计划后，应至少满足以下标准：

- 后台商品、销售、客户、认证、公开询价均有 API 集成测试
- 关键业务 use case 都有成功分支和至少 2 个失败分支
- Analytics 至少覆盖 range 解析、空数据和关键聚合结果
- 前端至少覆盖后台登录/改密/商品售出/客户操作的主要交互
- Playwright 至少覆盖 3 条完整跨层业务闭环

## Phase 1：补齐 Web API 集成测试

目标：先保护真实 HTTP 入口。

预计新增目录：

- `tests/SecondHandShop.WebApi.IntegrationTests/Controllers/AdminProductsControllerTests.cs`
- `tests/SecondHandShop.WebApi.IntegrationTests/Controllers/AdminProductSalesControllerTests.cs`
- `tests/SecondHandShop.WebApi.IntegrationTests/Controllers/InquiriesControllerTests.cs`
- `tests/SecondHandShop.WebApi.IntegrationTests/Controllers/AdminAuthControllerTests.cs`
- `tests/SecondHandShop.WebApi.IntegrationTests/Controllers/AdminCustomersControllerTests.cs`
- `tests/SecondHandShop.WebApi.IntegrationTests/Controllers/CategoriesControllerTests.cs`
- `tests/SecondHandShop.WebApi.IntegrationTests/Controllers/ProductsControllerTests.cs`
- `tests/SecondHandShop.WebApi.IntegrationTests/Controllers/AdminAnalyticsControllerTests.cs`

### 1.1 AdminProductsController

目标文件：

- [AdminProductsController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminProductsController.cs)

首批必须实现：

- `GET /api/lord/products` 未登录返回 `401`
- `POST /api/lord/products` 成功返回 `201`
- `POST /api/lord/products` 非法 `condition` 返回 `400`
- `PUT /api/lord/products/{id}/status` 非法 `status` 返回 `400`
- `PUT /api/lord/products/{id}/status` 合法状态更新返回 `204`
- `PUT /api/lord/products/{id}/featured` 返回 `204`
- `POST /api/lord/products/{id}/images/presigned-url` 返回 `200`
- `POST /api/lord/products/{id}/images` 返回 `204`
- `DELETE /api/lord/products/{productId}/images/{imageId}` 返回 `204`
- `GET /api/lord/products/{id}/categories` 返回 `200`
- `PUT /api/lord/products/{id}/categories` 返回 `200`

第二批补充：

- 已售商品通过普通状态接口恢复时返回冲突
- 非 Available 商品设为 featured 的错误映射
- 图片对象键归属错误映射

验收标准：

- 覆盖后台商品创建、状态变更、精选、图片、分类五类接口

### 1.2 AdminProductSalesController

目标文件：

- [AdminProductSalesController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminProductSalesController.cs)

首批必须实现：

- `GET /sale` 无当前销售时返回 `404`
- `POST /mark-sold` 成功返回 `201`
- `POST /revert-sale` 成功返回 `204`
- `POST /revert-sale` 非法 `reason` 返回 `400`
- `GET /sales` 返回历史列表
- `GET /inquiries` 产品不存在返回 `404`

第二批补充：

- 询价不属于商品时的错误映射
- 客户不存在时的错误映射
- 商品不存在时的错误映射

验收标准：

- 覆盖销售查询、售出、回滚、询价候选四类接口

### 1.3 InquiriesController

目标文件：

- [InquiriesController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/InquiriesController.cs)

首批必须实现：

- 成功提交返回 `201`
- 缺少 `TurnstileToken` 返回 `400`
- 非法邮箱返回 `400`
- 非法手机号返回 `400`
- 超长消息返回 `400`

第二批补充：

- Turnstile 失败时错误响应
- 频率限制命中时错误响应
- 请求 IP 解析验证

验收标准：

- 覆盖公开询价入口的成功、模型校验失败、业务拒绝三类路径

### 1.4 AdminAuthController

目标文件：

- [AdminAuthController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminAuthController.cs)

首批必须实现：

- `POST /login` 成功返回 `200` 且写 cookie
- `POST /refresh` 未认证返回 `401`
- `POST /refresh` 成功返回 `200` 且刷新 cookie
- `GET /me` 成功返回 `200`
- `POST /logout` 返回 `204` 且清 cookie
- `POST /change-initial-password` 成功后清 cookie

第二批补充：

- 无法解析 admin id 时 `refresh` 返回 `401`
- `me` 在账号不存在时返回 `401`

验收标准：

- 覆盖后台认证登录、续期、当前用户、登出、首次改密五类接口

### 1.5 AdminCustomersController

目标文件：

- [AdminCustomersController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminCustomersController.cs)

首批必须实现：

- `GET /api/lord/customers` 返回 `200`
- `GET /{customerId}` 客户不存在返回 `404`
- `POST /api/lord/customers` 成功返回 `201`
- `POST /api/lord/customers` 非法 `status` 返回 `400`
- `PATCH /{customerId}` 成功返回 `204`
- `GET /{customerId}/inquiries` 客户不存在返回 `404`
- `GET /{customerId}/sales` 客户不存在返回 `404`

第二批补充：

- 创建客户邮箱冲突返回 `409`
- 创建客户手机号冲突返回 `409`

验收标准：

- 覆盖客户列表、详情、创建、更新、子资源读取

### 1.6 CategoriesController 和 ProductsController

目标文件：

- [CategoriesController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/CategoriesController.cs)
- [ProductsController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/ProductsController.cs)

首批必须实现：

- `GET /api/categories` 返回活跃分类
- `GET /api/categories/tree` 返回树结构
- `POST /api/categories` 在管理员身份下成功返回 `201`
- `GET /api/products/search` 返回 `200`
- `GET /api/products/featured` limit clamp 生效
- `GET /api/products/{id}` 不存在返回 `404`
- `GET /api/products/slug/{slug}` 不存在返回 `404`

验收标准：

- 覆盖公开商品浏览入口和公开分类入口

### 1.7 AdminAnalyticsController

目标文件：

- [AdminAnalyticsController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminAnalyticsController.cs)

首批必须实现：

- 合法 range 返回 `200`
- 缺省 range 使用默认值
- 非法 range 返回 `400`

验收标准：

- 至少保护 analytics 控制器的输入解析与错误映射

## Phase 2：补齐 Application 单元测试

目标：补业务编排、输入分支和错误路径。

预计新增目录：

- `tests/SecondHandShop.Application.UnitTests/UseCases/Admin/ChangeAdminInitialPasswordCommandHandlerTests.cs`
- `tests/SecondHandShop.Application.UnitTests/UseCases/Admin/GetAdminMeQueryHandlerTests.cs`
- `tests/SecondHandShop.Application.UnitTests/UseCases/Customers/AdminCustomerServiceTests.cs`
- `tests/SecondHandShop.Application.UnitTests/UseCases/Categories/CreateCategoryCommandHandlerTests.cs`
- `tests/SecondHandShop.Application.UnitTests/UseCases/Categories/GetCategoryTreeQueryHandlerTests.cs`
- `tests/SecondHandShop.Application.UnitTests/UseCases/Catalog/ProductCategories/UpdateProductCategoriesCommandHandlerTests.cs`
- `tests/SecondHandShop.Application.UnitTests/UseCases/Catalog/ProductCategories/GetProductCategorySelectionQueryHandlerTests.cs`

### 2.1 改初始密码

目标文件：

- [ChangeAdminInitialPasswordCommandHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Admin/ChangeInitialPassword/ChangeAdminInitialPasswordCommandHandler.cs)

必须实现：

- 成功改密
- 新密码与确认不一致
- 当前密码错误
- 非强制改密账号禁止调用
- 新旧密码相同
- 成功后 `MustChangePassword` 为 `false`
- 成功后 `TokenVersion` 增加

### 2.2 GetAdminMe

目标文件：

- [GetAdminMeQueryHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Admin/Me/GetAdminMeQueryHandler.cs)

必须实现：

- 活跃管理员返回信息
- 不存在返回 `null`
- 停用账号返回 `null`

### 2.3 AdminCustomerService

目标文件：

- [AdminCustomerService.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Customers/AdminCustomerService.cs)

必须实现：

- 创建客户成功
- 邮箱冲突
- 电话冲突
- 创建时初始化状态与备注
- 更新客户成功
- 更新时手机号冲突
- 客户不存在

### 2.4 分类业务

目标文件：

- [CreateCategoryCommandHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Categories/CreateCategory/CreateCategoryCommandHandler.cs)
- [GetCategoryTreeQueryHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Categories/GetCategoryTree/GetCategoryTreeQueryHandler.cs)

必须实现：

- 空 name
- 空 slug
- slug 已存在
- 父分类不存在
- 循环层级检测
- 创建成功后 cache invalidate
- 树结构按 sortOrder 和 name 排序

### 2.5 商品分类分配

目标文件：

- [UpdateProductCategoriesCommandHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Catalog/ProductCategories/UpdateProductCategoriesCommandHandler.cs)
- [GetProductCategorySelectionQueryHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Catalog/ProductCategories/GetProductCategorySelectionQueryHandler.cs)

必须实现：

- 主分类自动纳入选择集
- 去重逻辑
- 空 Guid 过滤
- 非法分类 id 拒绝
- 删除旧分类关联
- 新分类关联追加
- 返回值包含主分类且顺序稳定

## Phase 3：补齐 Infrastructure 集成测试

目标：补数据库行为、复杂聚合和持久化约束。

预计新增目录：

- `tests/SecondHandShop.Infrastructure.IntegrationTests/Repositories/ProductSaleRepositoryTests.cs`
- `tests/SecondHandShop.Infrastructure.IntegrationTests/Repositories/AdminUserRepositoryTests.cs`
- `tests/SecondHandShop.Infrastructure.IntegrationTests/Services/AnalyticsServiceTests.cs`

### 3.1 ProductSaleRepository

目标文件：

- [ProductSaleRepository.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Infrastructure/Persistence/Repositories/ProductSaleRepository.cs)

必须实现：

- 当前销售读取
- 历史销售列表按时间倒序
- 按客户读取成交记录
- `GetByIdAsync` 读取
- `AddAsync` 持久化

### 3.2 AdminUserRepository

目标文件：

- [AdminUserRepository.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Infrastructure/Persistence/Repositories/AdminUserRepository.cs)

必须实现：

- 按用户名读取
- 按 id 读取
- 停用账号读取行为

### 3.3 AnalyticsService

目标文件：

- [AnalyticsService.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Infrastructure/Services/Analytics/AnalyticsService.cs)

首批必须实现：

- 空数据返回零值结果
- `Last30Days` 汇总结果正确
- `AllTime` 汇总结果正确
- SalesByCategory 正确
- DemandByCategory 正确
- SalesTrend 正确
- HotUnsoldProducts 正确
- StaleStockProducts 正确

验收标准：

- 至少验证 2 个 range 和 6 类聚合输出

## Phase 4：补齐前端组件测试

目标：保护后台高频交互和公开表单。

预计新增目录：

- `frontend/src/pages/__tests__/AdminLoginPage.test.tsx`
- `frontend/src/pages/__tests__/AdminChangePasswordPage.test.tsx`
- `frontend/src/pages/__tests__/InquiryPage.test.tsx`
- `frontend/src/pages/__tests__/AdminProductsPage.test.tsx`
- `frontend/src/pages/__tests__/AdminCustomersPage.test.tsx`
- `frontend/src/pages/__tests__/AdminAnalyticsPage.test.tsx`

### 4.1 后台认证

必须实现：

- 登录页提交成功跳转
- 登录失败提示
- 改密页校验不一致密码
- 改密成功后要求重新登录

### 4.2 商品与销售

必须实现：

- 商品页状态更新触发 API
- 商品售出后刷新列表
- 图片上传前的输入校验

### 4.3 客户管理

必须实现：

- 创建客户成功
- 冲突提示展示
- 编辑客户成功

### 4.4 Analytics 页面

必须实现：

- range 切换触发查询
- 空数据态展示
- 查询失败态展示

### 4.5 公开询价页

必须实现：

- 联系方式校验
- 提交成功提示
- 提交失败提示

## Phase 5：补齐 E2E 闭环测试

目标：补真正跨层业务闭环，只保留少量高价值路径。

目标文件：

- [admin-and-public-flows.spec.ts](D:/Projects/SecondHandShopWebsite/frontend/tests/e2e/admin-and-public-flows.spec.ts)

建议新增场景：

- 管理员登录后创建商品，前台商品列表可见
- 前台提交询价，后台客户详情页能看到询价记录
- 后台将商品标记售出，前台商品状态更新或不再出现在公开列表
- 强制改密账号登录后被限制，再完成改密并重新登录
- 创建客户时出现邮箱或手机号冲突提示

验收标准：

- 至少保留 3 条稳定闭环
- 每条闭环都穿过前端、API、应用层、数据库

## 推荐实施节奏

建议按四周推进。

### 第 1 周

- 完成 `AdminProductSalesControllerTests`
- 完成 `AdminProductsControllerTests`
- 完成 `InquiriesControllerTests`
- 完成 `AdminAuthControllerTests`

### 第 2 周

- 完成 `AdminCustomersControllerTests`
- 完成 `CategoriesControllerTests`
- 完成 `ProductsControllerTests`
- 完成 `ChangeAdminInitialPasswordCommandHandlerTests`
- 完成 `AdminCustomerServiceTests`

### 第 3 周

- 完成分类相关 Application 测试
- 完成 `ProductSaleRepositoryTests`
- 完成 `AdminUserRepositoryTests`
- 完成 `AnalyticsServiceTests`

### 第 4 周

- 完成前端核心组件测试
- 完成 3 条高价值 Playwright 闭环
- 统一清理测试夹具和命名

## Definition of Done

每个测试批次完成时必须满足：

- 新测试全部通过
- 原有测试全部通过
- 测试名称明确表达业务行为
- 成功路径和关键失败路径都被覆盖
- 不为了测试而修改生产逻辑语义
- 公共测试夹具被复用，避免复制粘贴

## 建议执行命令

```powershell
dotnet test tests\SecondHandShop.Domain.UnitTests\SecondHandShop.Domain.UnitTests.csproj
dotnet test tests\SecondHandShop.Application.UnitTests\SecondHandShop.Application.UnitTests.csproj
dotnet test tests\SecondHandShop.Infrastructure.IntegrationTests\SecondHandShop.Infrastructure.IntegrationTests.csproj
dotnet test tests\SecondHandShop.WebApi.IntegrationTests\SecondHandShop.WebApi.IntegrationTests.csproj
npm --prefix frontend run test
npm --prefix frontend run test:e2e
```

## 最终优先级结论

最先做的不是增加更多 Repository 测试，而是优先保护真实业务入口：

1. 后台销售 API
2. 后台商品管理 API
3. 公开询价 API
4. 后台认证完整会话流
5. 客户后台管理
6. Analytics

只有这几块稳定后，前端和 E2E 的测试投入才会更划算。
