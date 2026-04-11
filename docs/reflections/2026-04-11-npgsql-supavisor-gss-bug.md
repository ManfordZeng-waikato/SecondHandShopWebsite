# 反思：一次 Npgsql + Supabase 连接异常的误判复盘

**日期**：2026-04-11
**影响范围**：管理端"创建商品后上传图片"接口持续 500
**最终根因**：Npgsql 10.0.x 在与 Supabase Supavisor pooler 协商 GSS 加密时触发的上游竞态
**最终修复**：在连接串构造器里针对 `.pooler.supabase.com` host 设置 `GssEncryptionMode=Disable`（单行改动）

---

## 一句话复盘

一个单行配置就能解决的问题，被误判成"并发冲突 → 连接池 bug → 版本回归 → Supavisor session vs transaction 模式"一连串方向，前后做了四次基本无效的修改，才回到正确路径。根本原因是**没有在第一时间核实运行环境的真实配置**，以及**过度相信表层症状**。

---

## 事情的真实发展顺序

| 阶段 | 现象 | 采取的判断 | 动作 | 效果 |
|---|---|---|---|---|
| 1 | 前端收到 409 "data conflict"，后端日志显示 `DbUpdateException` 包着 `ObjectDisposedException: ManualResetEventSlim` | 判定是 Npgsql 10.0.2 的 bug | 降级 `Npgsql` / `Npgsql.EntityFrameworkCore.PostgreSQL` 到 10.0.0 + 修 `ApiExceptionFilter` 把非并发的 `DbUpdateException` 落到 500 | 409 的误伤修好了，但 500 依然复现 |
| 2 | 500 依然是同样的 `ObjectDisposedException` | 判定是连接池复用坏 connector | 在 `PostgresConnectionStringResolver` 里加 `ApplyLocalhostDefaults`，对 localhost 关闭 Pool | 无效——没意识到用户根本不在连 localhost |
| 3 | 同样报错，请求耗时从 1.1s 变成 8.7s | 意识到延迟不对 | 用 `dotnet user-secrets list` 去查真实连接串 | 发现真实 host 是 `aws-1-ap-southeast-2.pooler.supabase.com`，Supavisor pooler + 国内到悉尼的高延迟链路 |
| 4 | 命中 issue #6415 描述的"高延迟 + Supavisor pooler 100% 复现" | 建议用户切换到 Supavisor transaction 模式（端口 6543） | 用户改端口 | 无效 |
| 5 | 重新精读 issue #6415，发现 Npgsql 维护者的一句关键说明：Supavisor 不支持 GSS session 加密，Npgsql 协商时被粗暴关连接，进而清 pool、dispose MRES | 定位到根因在连接启动的协议协商阶段，不在池化和命令执行路径 | `GssEncryptionMode=Disable` | 一次成功 |

从第 1 步到第 5 步，中间跨了四次修改、三次重启、两次无效的 workaround。核心问题是**直到第 3 步才去读真实的连接串**。

---

## 第一个也是最致命的错误：没有核实运行环境

前两次修改都建立在一个**被默认却从未验证**的假设上——用户连的是 localhost 或者某种本地 Postgres。这个假设来自 `CLAUDE.md` 里写的开发默认连接串：

```
Host=localhost;Database=SecondHandShopDb;Username=postgres;Password=postgres;
```

但 `appsettings.Development.json` 里 `ConnectionStrings:DefaultConnection` 是一个空字符串，真实值在 **user secrets** 里。`CLAUDE.md` 描述的只是"默认"——不是用户当前的实际配置。

**教训**：当项目文档里的"默认配置"和 `appsettings*.json`、`appsettings.*.json`、user secrets、环境变量可能互相覆盖时，**在做任何环境相关的判断之前，必须先用 `dotnet user-secrets list` 或直接在代码里打印解析后的 connection string 来确认到底连的是哪里**。这一步应该在日志里看到"ObjectDisposedException"的第一秒就做，而不是做到第三次修改才想起来。

如果第一步就看了真实连接串——`aws-1-ap-southeast-2.pooler.supabase.com`——我会立刻意识到这是远程 Supabase + 远距离链路，直接去查 Supabase + Npgsql 10 的已知问题，完全不会走 "Pooling=false 关本地池" 这种完全无关的歪路。

---

## 第二个错误：过度相信搜索到的第一条 workaround

Issue #6415 的搜索摘要里第一条 workaround 是"降级到 10.0.0"。我就直接照做了，却没有读完整条 issue。实际上同一条 issue 的后面：

- `Pooling=false` —— **已被试过，无效**
- `NoResetOnClose=true` —— **已被试过，无效**
- `CancellationTimeout=0` —— **已被试过，无效**
- `Multiplexing=false` —— **已被试过，无效**
- 降级到 10.0.0 —— **部分用户有效**，并不是普适
- 真正的根因描述和 GSS 的关联 —— **埋在维护者的一条评论里**

**教训**：搜索引擎摘要会按"看起来像答案"的片段排序，不会告诉你什么已经试过但无效。读 GitHub issue 时不能只看第一段，**必须把所有 workaround 列出来逐条排除**，尤其要找维护者的 root-cause 评论。这次在第二次 WebFetch 的时候专门问了"列出所有 workaround + 维护者对 root cause 的评论"，才终于看到 GSS 那一句话。这个问法应该是第一次就该用的。

---

## 第三个错误：把症状当原因，做"表面通顺"的修复

第 2 步加的 `ApplyLocalhostDefaults` 其实从逻辑上是讲得通的——它沿用了项目里已有的 `ApplySupabasePoolerDefaults` 思路，文字注释也写得头头是道。但它**根本没有回答"为什么这个端点会失败而其它端点都成功"这个核心问题**。如果池复用是根因，那创建商品也应该同样概率失败，为什么偏偏只有 AddImage？

真实原因是 `AddProductImageAsync` 在一次 DbContext 里连着做了 3 次 DB 往返（2 次 SELECT + 1 次 SaveChanges），而其它端点大多是"一次查询 + 一次 SaveChanges"或更简单的形状。AddImage 里恰好第一次 SELECT 的连接启动阶段触发 GSS 协商 → Supavisor 关连接 → Npgsql 清 pool → 同一个 DbContext 后续 SaveChanges 命中已被 dispose 的 connector 内部状态。**这个"为什么只有这个端点"的问题从一开始就摆在那，但我没深究**。

**教训**：当一个 workaround 的逻辑能"解释症状"但不能"解释为什么只影响 A 而不影响 B"时，它大概率是错的。遇到局部复现的问题，第一个要回答的问题永远是"受影响的代码路径 vs 不受影响的代码路径有什么不同"。

---

## 第四个错误：让异常过滤器掩盖了真实异常

`ApiExceptionFilter.cs` 里原本有这么一条：

```csharp
DbUpdateException =>
    (StatusCodes.Status409Conflict,
     "A data conflict occurred while saving changes. Please try again."),
```

它把所有 `DbUpdateException`（包括真正的并发冲突、唯一键违反、外键违反、驱动层故障）**不加区分地**都映射成 409 "并发冲突"。

这导致第一次看日志时，我被"client error 409"和"data conflict"这两个字眼牵着走，一度以为是 `xmin` 乐观锁出了问题。实际上真正的 inner exception 是 `ObjectDisposedException`，跟并发一点关系都没有。

第 1 步修复时我已经把这条规则收紧了——只有真正的 `DbUpdateConcurrencyException` 才返回 409，其它 `DbUpdateException` 落到 500 并打完整 stack。但这个问题**早在这个 filter 被写下那一天就埋好了**，这次只是它的首次爆发。

**教训**：异常到 HTTP 状态码的映射是"信息输出"层面的事情，不能为了"用户看起来友好"而丢掉类型信息。**任何一个 `catch (BaseException)` 形式的 mapping 都要问一遍：这个父类的所有子类是不是都应该映射到同一个语义？** 如果不是，就必须拆开。尤其是像 `DbUpdateException` 这种横跨"业务冲突"和"驱动故障"两种完全不同语义的父类型，一刀切极其危险。

---

## 这一条 bug 真正教会了我什么

1. **核实运行环境永远比猜测运行环境便宜**。第一次看到 `ObjectDisposedException` 的时候，`dotnet user-secrets list` 只需要 2 秒就能跑完，而我因为没跑这一条命令，多花了至少三轮修改。
2. **Workaround 和 root cause 是两件事**。issue 搜索的摘要里混在一起的各种建议，本质上是不同作者在不同环境下试出来的"看起来有效的办法"——它们不一定对应同一个根因，也不一定适用于你。**找到维护者对根因的表述**远比"复制粘贴一个 workaround 看看行不行"有效。
3. **错误映射要保持类型信息**。用户友好的错误文案应该是**在正确的类型判断之后加上去的**，而不是为了"少写几个 case"把一堆父类型压成同一个出口。
4. **先解释"为什么是这一个端点"，再提方案**。局部性问题天然地在告诉你"差异在哪"，忽略它就是在主动丢弃最有价值的诊断信息。
5. **调试过程里的"耗时变化"是一条免费信号**。第 2 步之后请求耗时从 1.1s 跳到 8.7s 却没能让我立刻意识到"连接目标变了"，这其实是最早的"你在连远程"的硬证据。以后看到请求耗时量级突变，都要当作单独一条线索追一次。

---

## 最终修复的完整构成

按修改的时间顺序，但真正起作用的是最后一条：

| 文件 | 改动 | 是真修还是误伤修复 |
|---|---|---|
| `src/SecondHandShop.WebApi/Filters/ApiExceptionFilter.cs` | 只把 `DbUpdateConcurrencyException` 映射成 409，其它 `DbUpdateException` 落 500 打完整异常 | **值得保留**，是潜伏的信息丢失 bug 被暴露后的正经修复 |
| `src/SecondHandShop.Infrastructure/SecondHandShop.Infrastructure.csproj` / `SecondHandShop.WebApi.csproj` | `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1 → 10.0.0，显式钉 `Npgsql 10.0.0`，删掉 WebApi 冗余的显式 `Npgsql` 引用，消除版本漂移 | **值得保留**，版本漂移本身是 latent risk，钉住版本并加 issue 编号注释让未来升级有据可依 |
| `src/SecondHandShop.Infrastructure/Persistence/PostgresConnectionStringResolver.cs` `ApplyLocalhostDefaults` | 对 localhost 关 Pool | **可以留也可以删**。没解决这次的问题，但对"未来切回本地 PG 开发时的防御"有一丁点价值。属于"顺手加的保险"，保留 cost 很低 |
| `src/SecondHandShop.Infrastructure/Persistence/PostgresConnectionStringResolver.cs` `ApplySupabasePoolerDefaults` 追加 `GssEncryptionMode=Disable` | **真修**。直接消除 Npgsql + Supavisor 的 GSS 协商这条触发路径 | **必须保留**，直到 Npgsql 上游修掉 issue #6415 |

最后一行是真正解决问题的那一行。前面的改动里，`ApiExceptionFilter` 和版本钉死是"顺手修掉的其它问题"，`ApplyLocalhostDefaults` 则是"这一次其实没用上的防御性修复"。

---

## 给未来自己的行动清单

下次再看到一个"数据库相关的诡异异常"时，按这个顺序走：

1. **先确认连的是哪里**。跑 `dotnet user-secrets list`，或者在启动时打印解析后的连接串（mask 掉密码）。不要相信 `CLAUDE.md` 或 `appsettings.json` 里写的"默认"。
2. **先读完整的 inner exception**。如果看到 `DbUpdateException`，立刻找 `--->` 后面的真实类型。不要相信 API 返回的 "data conflict" 这种人类友好文案。
3. **先问"为什么是这一条代码路径"**。失败的端点和成功的端点，代码形状上有什么不同？差异里就藏着根因。
4. **找 root cause 评论，而不是 workaround 列表**。搜索 issue 时直接问 AI："列出所有 workaround 和维护者对 root cause 的评论"，不要只看摘要第一条。
5. **耗时量级变化是硬信号**。请求耗时出现数量级跳变时，先问"目标是不是变了、链路是不是变了"，再继续下一步修改。

---

**相关上游 issue**：
- [npgsql/npgsql#6415 — ObjectDisposedException in ManualResetEventSlim.Reset() during runtime queries](https://github.com/npgsql/npgsql/issues/6415)
- [npgsql/efcore.pg#3699 — ObjectDisposedException in ManualResetEventSlim when running dotnet ef database update with .NET 10 and Npgsql 10.0.0](https://github.com/npgsql/efcore.pg/issues/3699)
