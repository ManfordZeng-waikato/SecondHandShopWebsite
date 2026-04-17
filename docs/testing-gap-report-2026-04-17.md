# SecondHandShop 测试业务覆盖分析报告

日期：2026-04-17

## 结论摘要

当前测试框架更接近“基础能力回归网”，还不是完整的业务覆盖体系。

- Repository 和部分应用层规则覆盖相对较好。
- Web API 集成测试覆盖明显不足。
- 前端组件和 E2E 只覆盖了极少数烟雾路径。
- 核心后台业务中，销售、商品管理、客户管理、认证会话完整生命周期、Analytics 仍存在明显测试空白。

从业务覆盖角度看：

- 基础设施层：中等
- 应用层：中等偏下
- Web API 层：低
- 前端组件层：低
- E2E 层：低

## 当前测试分布

截至本次盘点，项目中的测试大致分布如下：

- 领域层：5 个
- 应用层：22 个
- 基础设施集成层：29 个
- Web API 集成层：3 个
- 前端组件测试：3 个
- E2E：3 个

总量约 65 个测试。

## 当前已覆盖的业务

### 1. 询价

已覆盖：

- 有效询价提交
- 不可询价商品拒绝提交
- IP 反垃圾封禁与 cooldown 持久化

对应测试：

- [InquiryServiceTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Application.UnitTests/UseCases/Inquiries/InquiryServiceTests.cs)

### 2. 后台登录与会话续期

已覆盖：

- 登录成功
- 密码错误计数
- 锁定延长
- 活跃管理员续期成功
- 停用或不存在管理员续期失败

对应测试：

- [LoginAdminCommandHandlerTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Application.UnitTests/UseCases/Admin/LoginAdminCommandHandlerTests.cs)
- [RefreshAdminSessionCommandHandlerTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Application.UnitTests/UseCases/Admin/RefreshAdminSessionCommandHandlerTests.cs)

### 3. 客户解析

已覆盖：

- 邮箱命中时合并到现有客户
- 邮箱与电话映射不同客户时抛冲突
- 无匹配时创建客户

对应测试：

- [CustomerResolutionServiceTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Application.UnitTests/UseCases/Customers/CustomerResolutionServiceTests.cs)

### 4. 销售流程

已覆盖：

- 通过买家联系方式自动匹配/创建客户
- 错误的询价归属被拒绝
- 非法支付方式被拒绝
- 已售商品回滚成功
- 未售商品禁止回滚

对应测试：

- [AdminSaleServiceTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Application.UnitTests/UseCases/Sales/AdminSaleServiceTests.cs)

### 5. 后台商品状态与图片管理

已覆盖：

- `OffShelf -> Available`
- 禁止直接把状态设为 `Sold`
- 已售商品禁止直接恢复为 `Available`
- 图片对象键归属校验
- 新主图写入后封面图与图片数量同步

对应测试：

- [AdminCatalogServiceTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Application.UnitTests/UseCases/Catalog/AdminCatalogServiceTests.cs)

### 6. Repository 集成

已覆盖：

- ProductRepository：公开查询、分页、分类过滤、精选商品、分类加载
- InquiryRepository：IP/Email/Hash 查询、cooldown upsert、持久化
- CustomerRepository：邮箱/手机号查询、唯一约束、详情投影
- CategoryRepository：slug 查询、活跃列表、按 id 批量查询、新增

对应测试：

- [ProductRepositoryTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Infrastructure.IntegrationTests/Repositories/ProductRepositoryTests.cs)
- [InquiryRepositoryTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Infrastructure.IntegrationTests/Repositories/InquiryRepositoryTests.cs)
- [CustomerRepositoryTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Infrastructure.IntegrationTests/Repositories/CustomerRepositoryTests.cs)
- [CategoryRepositoryTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.Infrastructure.IntegrationTests/Repositories/CategoryRepositoryTests.cs)

### 7. Web API 集成

已覆盖：

- 后台未登录访问产品接口返回 `401`
- 强制改密 token 访问后台产品接口返回 `403`
- 登出清理后台 cookie

对应测试：

- [AdminAuthAndAuthorizationTests.cs](D:/Projects/SecondHandShopWebsite/tests/SecondHandShop.WebApi.IntegrationTests/Controllers/AdminAuthAndAuthorizationTests.cs)

### 8. 前端组件与 E2E

已覆盖：

- 销售弹窗负数价格校验
- 销售弹窗买家信息提示
- 销售弹窗成功提交时 payload trim
- 后台登录页可进入产品页
- 前台商品页可打开
- 询价页基础联系信息校验

对应测试：

- [ProductSaleDialog.test.tsx](D:/Projects/SecondHandShopWebsite/frontend/src/features/admin/components/__tests__/ProductSaleDialog.test.tsx)
- [admin-and-public-flows.spec.ts](D:/Projects/SecondHandShopWebsite/frontend/tests/e2e/admin-and-public-flows.spec.ts)

## 主要缺口清单

以下按重要程度排序。

## P0：必须优先补齐

### 1. 后台商品销售 API 合同测试几乎为空

涉及代码：

- [AdminProductSalesController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminProductSalesController.cs)

缺少场景：

- `POST /api/lord/products/{productId}/mark-sold` 成功返回 `201`
- `POST /revert-sale` 非法取消原因返回 `400`
- 客户不存在、询价不存在、商品不存在时的响应映射
- `GET /sale` 在有当前成交和无当前成交时的分支
- `GET /sales` 历史列表返回正确内容
- `GET /inquiries` 在产品不存在时返回 `404`

业务风险：

- 销售是后台核心动作，目前只覆盖了应用层逻辑，没有覆盖 HTTP 契约、模型绑定、鉴权、响应体和错误映射。

### 2. 后台商品管理 API 合同测试为空

涉及代码：

- [AdminProductsController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminProductsController.cs)

缺少场景：

- 创建商品成功
- slug 冲突
- 无效 `Condition` 返回 `400`
- 更新状态成功
- 无效 `Status` 返回 `400`
- 已售商品禁止通过普通状态接口恢复
- 更新精选成功
- 精选排序越界
- 非 `Available` 商品禁止设为精选
- 图片预签名 URL 接口合同
- 添加图片接口合同
- 删除图片接口合同
- 获取商品分类选择
- 更新商品分类选择

业务风险：

- 商品后台管理是最高频后台操作之一，目前缺少任何控制器级回归保护。

### 3. 公开询价 API 缺少集成测试

涉及代码：

- [InquiriesController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/InquiriesController.cs)

缺少场景：

- 成功创建询价返回 `201`
- 缺少 Turnstile token
- 非法邮箱
- 非法手机号
- 超长消息
- Turnstile 校验失败时的错误响应
- 频率限制命中后的响应码与错误体
- 客户 IP 解析行为

业务风险：

- 询价是唯一公开转化入口之一，当前只验证了应用层，不足以保护实际 HTTP 请求行为。

### 4. 后台认证与会话完整生命周期缺少覆盖

涉及代码：

- [AdminAuthController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminAuthController.cs)
- [ChangeAdminInitialPasswordCommandHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Admin/ChangeInitialPassword/ChangeAdminInitialPasswordCommandHandler.cs)
- [GetAdminMeQueryHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Admin/Me/GetAdminMeQueryHandler.cs)

缺少场景：

- 登录成功写入 cookie 和 session header
- `refresh` 成功续签并刷新 cookie
- `me` 成功返回当前管理员
- `me` 在账号停用或不存在时返回未授权
- 首次改密成功后清 cookie
- 新旧密码相同
- 确认密码不一致
- 非强制改密账号调用该接口被拒绝
- 改密成功后 `MustChangePassword` 清除且 `TokenVersion` 增长

业务风险：

- 当前只测了“未登录/强制改密/登出”三条外围路径，没有覆盖后台真实会话生命周期。

### 5. 客户后台管理几乎没有业务测试

涉及代码：

- [AdminCustomerService.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Customers/AdminCustomerService.cs)
- [AdminCustomersController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminCustomersController.cs)

缺少场景：

- 创建客户成功
- 创建客户时邮箱冲突
- 创建客户时手机号冲突
- 创建时状态和备注初始化
- 更新客户成功
- 更新客户时手机号与其他客户冲突
- 获取详情不存在返回 `404`
- 获取客户询价列表不存在返回 `404`
- 获取客户成交列表不存在返回 `404`
- 非法客户状态返回 `400`

业务风险：

- 客户模块已承载询价和成交链路，但目前缺少应用层和 API 层回归网。

### 6. Analytics 完全无测试

涉及代码：

- [AnalyticsService.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Infrastructure/Services/Analytics/AnalyticsService.cs)
- [AdminAnalyticsController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/AdminAnalyticsController.cs)

缺少场景：

- `range=7d/30d/90d/12m/all` 解析正确
- 非法 range 返回 `400`
- 汇总指标计算正确
- 类别销量榜正确
- 需求榜正确
- 销售趋势正确
- 热销未售商品正确
- 滞销库存列表正确
- 空数据场景

业务风险：

- Analytics 查询复杂、聚合多、最容易在 schema 或 LINQ 变更时静默出错，目前没有任何保护。

## P1：应尽快补齐

### 1. 分类业务测试不足

涉及代码：

- [CreateCategoryCommandHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Categories/CreateCategory/CreateCategoryCommandHandler.cs)
- [GetCategoryTreeQueryHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Categories/GetCategoryTree/GetCategoryTreeQueryHandler.cs)
- [CategoriesController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/CategoriesController.cs)

缺少场景：

- 空 name
- 空 slug
- 重复 slug
- 父分类不存在
- 循环层级检测
- 树形排序
- 创建后缓存失效
- 公开列表与树接口合同

### 2. 商品分类分配无测试

涉及代码：

- [UpdateProductCategoriesCommandHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Catalog/ProductCategories/UpdateProductCategoriesCommandHandler.cs)
- [GetProductCategorySelectionQueryHandler.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Application/UseCases/Catalog/ProductCategories/GetProductCategorySelectionQueryHandler.cs)

缺少场景：

- 主分类自动加入选择集
- 去重逻辑正确
- 空 Guid 被过滤
- 非法分类 ID 拒绝
- 删除旧关联、追加新关联
- 查询时包含主分类并保持排序稳定

### 3. 公开商品 API 缺少控制器级覆盖

涉及代码：

- [ProductsController.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.WebApi/Controllers/ProductsController.cs)

缺少场景：

- 搜索 fallback 行为
- featured limit clamp
- `GetById` 和 `GetBySlug` 的 `404`
- 图片 URL 映射
- 类别名映射
- slug 标准化

### 4. 领域规则覆盖仍然偏薄

涉及代码：

- [Product.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Domain/Entities/Product.cs)
- [ProductSale.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Domain/Entities/ProductSale.cs)
- [AdminUser.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Domain/Entities/AdminUser.cs)
- [Inquiry.cs](D:/Projects/SecondHandShopWebsite/src/SecondHandShop.Domain/Entities/Inquiry.cs)

缺少场景：

- Product 精选排序边界
- Product 非法恢复路径
- Product `UpdateDetails` 的 slug 和 price 规则
- ProductSale 二次取消保护
- ProductSale 输入 trim 行为
- AdminUser 成功登录重置失败计数
- AdminUser 停用 bump token version
- AdminUser 完成强制改密
- Inquiry 联系方式必填
- Inquiry 邮箱与手机号格式校验
- Inquiry 邮件投递状态迁移

## P2：建议补齐

### 1. Repository 集成仍有空白

缺少：

- `ProductSaleRepository`
- `AdminUserRepository`
- 后台分页/排序/过滤查询
- `InquiryRepository.AcquireAntiSpamConcurrencyLocksAsync`
- `CustomerRepository.ListPagedForAdminAsync`

### 2. 前端组件测试覆盖极低

缺少：

- 登录页
- 改密页
- 受保护后台路由
- 商品创建/编辑/上下架
- 图片上传
- 客户创建/编辑
- 客户详情页
- Analytics 页 range 切换和空态
- 询价表单提交成功/失败

### 3. E2E 只有烟雾测试，没有关键业务闭环

缺少：

- 后台创建商品后前台可见
- 前台提交询价后后台可查看客户/询价
- 后台标记售出后前台状态变化
- 强制改密账号登录并完成改密
- 客户创建或编辑冲突提示

## 建议补测顺序

建议执行顺序如下：

1. 先补 `WebApi.IntegrationTests`
2. 优先覆盖 `AdminProductSalesController`、`AdminProductsController`、`InquiriesController`、`AdminAuthController`
3. 再补 `Application.UnitTests` 中的改密、客户管理、分类、商品分类分配
4. 然后补 `AnalyticsService` 和 `ProductSaleRepository` 的集成测试
5. 再扩展前端后台管理组件测试
6. 最后补 3 到 5 条真正跨层闭环的 Playwright 场景

## 推荐的优先实现 backlog

### 第一批

- `AdminProductSalesController` 集成测试
- `AdminProductsController` 集成测试
- `InquiriesController` 集成测试
- `AdminAuthController` 集成测试

### 第二批

- `ChangeAdminInitialPasswordCommandHandler` 单元测试
- `AdminCustomerService` 单元测试
- `CreateCategoryCommandHandler` 单元测试
- `UpdateProductCategoriesCommandHandler` 单元测试
- `GetProductCategorySelectionQueryHandler` 单元测试

### 第三批

- `AnalyticsService` 集成测试
- `ProductSaleRepository` 集成测试
- `AdminUserRepository` 集成测试

### 第四批

- 后台登录/改密/商品管理/客户管理 React 组件测试
- 关键业务闭环 Playwright 测试

## 一句话结论

当前项目在 Repository 查询和少量应用层规则上已经有一定基础，但核心后台业务的 API 合同、认证生命周期、客户管理、Analytics，以及前后端闭环测试仍明显不足。最优先补的不是更多 Repository 测试，而是直接保护真实业务入口的 Web API 集成测试和关键后台流程。
