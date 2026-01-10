# Research: Most Commonly Used .NET Interfaces

## Goal

Identify the 50 most commonly used .NET interfaces to prioritize KnockOff stub generation support. Interfaces like `IList<T>`, `IDictionary<K,V>`, `IEnumerable<T>`, etc. are candidates for built-in stub implementations or specialized handling.

## Task List

- [x] Search for existing academic research
- [x] Search for empirical GitHub/codebase analysis
- [x] Search for mocking library usage patterns
- [x] Run BigQuery analysis on GitHub C# dataset → See [bigquery-interface-analysis.md](bigquery-interface-analysis.md)
- [ ] Analyze dotnet/runtime for interface implementation counts
- [ ] Compile final ranked list of top 50 interfaces

---

## Research Findings

### 1. Academic Research

#### Microsoft Research: UP-Miner
- **Paper**: [Mining Succinct and High-Coverage API Usage Patterns from Source Code](https://www.microsoft.com/en-us/research/publication/mining-succinct-and-high-coverage-api-usage-patterns-from-source-code/)
- Analyzed large-scale Microsoft codebase for API usage patterns
- Focused on method call sequences rather than interface rankings

#### .NET Framework Feature Adoption Study (2015)
- **Paper**: [Towards an Empirical Analysis of .NET Framework and C# Language Features' Adoption](https://ieeexplore.ieee.org/abstract/document/7424222/)
- Key finding: "Currently, there is little or no empirical analysis of the adoption of .NET framework and C# programming language features"
- Found C# generics adoption exceeds Java's
- Did not produce interface-specific rankings

#### General API Usability Studies
- [An Empirical Study on API Usages](https://ieeexplore.ieee.org/document/8186224/) - IEEE
- Focus on API call patterns, not interface frequency

### 2. BigQuery Analysis of GitHub C# Code

**Source**: [Matt Warren - Analysing C# code on GitHub with BigQuery](https://mattwarren.org/2017/10/12/Analysing-C-code-on-GitHub-with-BigQuery/) (2017)

**Dataset**: `fh-bigquery:github_extracts.contents_net_cs` (~5.8 million C# files)

#### Most Used Namespaces (by file count)
| Namespace | Files |
|-----------|-------|
| NUnit.Framework | 119,463 |
| UnityEngine | 117,673 |
| Xunit | 99,099 |
| Newtonsoft.Json | 81,675 |
| Moq | 23,546 |
| log4net | 17,297 |

#### Most Popular NuGet Packages
| Package | Count |
|---------|-------|
| Newtonsoft.Json | 45,055 |
| EntityFramework | 14,191 |
| NUnit | 10,341 |
| jQuery | 10,646 |

#### Most Thrown Exception Types
| Exception | Instances |
|-----------|-----------|
| ArgumentNullException | 699,526 |
| ArgumentException | 361,616 |
| NotImplementedException | 340,361 |
| InvalidOperationException | 260,792 |

#### Other Statistics
- 218,643 files use `async`/`await`
- 1,457,154 files use `var` keyword
- 712,498 files contain `#region`

**Limitation**: Analysis focused on namespaces, packages, and exceptions—not interface implementations specifically.

### 2b. Our BigQuery Analysis (January 2026)

**Dataset**: `bigquery-public-data.github_repos.sample_contents` (10% sample of popular repos, ~23GB)

#### Non-Generic Interface Implementations (`: IFoo`)

| Rank | Interface | Count | Category |
|------|-----------|-------|----------|
| 1 | IDisposable | 1,177 | Lifecycle |
| 2 | IEquatable | 434 | Comparison |
| 3 | IValueConverter | 330 | WPF/XAML |
| 4 | IEnumerable | 280 | Collections |
| 5 | IEnumerator | 242 | Collections |
| 6 | INotifyPropertyChanged | 230 | Notifications |
| 7 | IComparable | 211 | Comparison |
| 8 | IComparer | 201 | Comparison |
| 9 | IMessage | 200 | Protobuf/Messaging |
| 10 | ICoreAnnotation | 178 | Stanford NLP |
| 11 | IEqualityComparer | 157 | Comparison |
| 12 | ICommand | 140 | WPF/MVVM |
| 13 | ICloneable | 114 | Cloning |
| 14 | ICollection | 105 | Collections |
| 15 | IList | 103 | Collections |
| 16 | IFoo | 93 | Test/Example |
| 17 | IMigrationMetadata | 70 | EF Migrations |
| 18 | IDictionary | 69 | Collections |
| 19 | ILogger | 68 | Logging |
| 20 | IServiceOperations | 63 | Azure SDK |
| 21 | ISerializable | 52 | Serialization |
| 28 | IServiceProvider | 36 | DI |
| 37 | INotifyPropertyChanging | 37 | Notifications |
| 39 | IFormattable | 26 | Formatting |
| 40 | IXmlSerializable | 24 | Serialization |

#### Generic Interface Implementations (`: IFoo<T>`)

| Rank | Interface | Count | Category |
|------|-----------|-------|----------|
| 1 | IEquatable<T> | 432 | Comparison |
| 2 | IEnumerable<T> | 211 | Collections |
| 3 | IEnumerator<T> | 190 | Collections |
| 4 | ICoreAnnotation<T> | 178 | Stanford NLP |
| 5 | IEqualityComparer<T> | 140 | Comparison |
| 6 | IComparer<T> | 133 | Comparison |
| 7 | IComparable<T> | 109 | Comparison |
| 8 | IMessage<T> | 96 | Protobuf |
| 9 | IList<T> | 76 | Collections |
| 11 | ICollection<T> | 61 | Collections |
| 12 | IDictionary<K,V> | 58 | Collections |
| 18 | IObserver<T> | 17 | Reactive |
| 26 | IObservable<T> | 13 | Reactive |
| 38 | ISet<T> | 10 | Collections |
| 39 | IReadOnlyList<T> | 9 | Collections |

#### Combined Rankings (BCL Interfaces Only)

| Rank | Interface | Total | Notes |
|------|-----------|-------|-------|
| 1 | **IDisposable** | 1,177 | Clear winner |
| 2 | **IEquatable<T>** | 866 | 434 + 432 |
| 3 | **IEnumerable/IEnumerable<T>** | 491 | 280 + 211 |
| 4 | **IEnumerator/IEnumerator<T>** | 432 | 242 + 190 |
| 5 | **IComparer/IComparer<T>** | 334 | 201 + 133 |
| 6 | **IComparable/IComparable<T>** | 320 | 211 + 109 |
| 7 | **IEqualityComparer/IEqualityComparer<T>** | 297 | 157 + 140 |
| 8 | **INotifyPropertyChanged** | 230 | UI binding |
| 9 | **IList/IList<T>** | 179 | 103 + 76 |
| 10 | **ICollection/ICollection<T>** | 166 | 105 + 61 |
| 11 | **ICommand** | 140 | MVVM |
| 12 | **IDictionary/IDictionary<K,V>** | 127 | 69 + 58 |
| 13 | **ICloneable** | 114 | Legacy |
| 14 | **ILogger** | 68 | Logging |
| 15 | **ISerializable** | 52 | Legacy |
| 16 | **INotifyPropertyChanging** | 37 | UI binding |
| 17 | **IServiceProvider** | 36 | DI |
| 18 | **IObserver<T>** | 17 | Reactive |
| 19 | **IObservable<T>** | 13 | Reactive |
| 20 | **ISet<T>** | 10 | Collections |
| 21 | **IReadOnlyList<T>** | 9 | Collections |

#### Interface Usage as Types (Fields, Parameters, Returns)

| Rank | Interface | Count | Category |
|------|-----------|-------|----------|
| 1 | IEnumerable | 14,863 | Collections |
| 2 | IList | 5,253 | Collections |
| 3 | IDictionary | 3,168 | Collections |
| 4 | ICollection | 1,641 | Collections |
| 5 | IAsyncResult | 1,328 | Async (legacy) |
| 6 | IEnumerator | 1,304 | Collections |
| 7 | IContainer | 1,151 | DI/Components |
| 8 | ILog | 886 | Logging |
| 9 | IFormatProvider | 727 | Formatting |
| 10 | ILogger | 697 | Logging |
| 11 | ICommand | 628 | MVVM |
| 12 | IReadOnlyList | 557 | Collections |
| 13 | IQueryable | 521 | LINQ/EF |
| 14 | IClientService | 505 | Google APIs |
| 15 | IObservable | 495 | Reactive |
| 17 | IDisposable | 450 | Lifecycle |
| 19 | IServiceProvider | 387 | DI |
| 21 | IComparer | 360 | Comparison |
| 22 | IEqualityComparer | 323 | Comparison |
| 27 | ISet | 266 | Collections |
| 31 | IDataReader | 231 | Data Access |
| 35 | IRepository | 217 | Repository Pattern |
| 41 | IActionResult | 176 | ASP.NET MVC |
| 42 | IAppBuilder | 173 | OWIN |
| 45 | IDbConnection | 161 | Data Access |
| 47 | ISession | 152 | NHibernate/Identity |
| 49 | IDbCommand | 144 | Data Access |

### 3. Mocking Library Patterns

**Common mocking frameworks**: Moq, NSubstitute, FakeItEasy

**Interfaces commonly shown in mocking examples**:
- `IUserService`, `IRepository<T>` - business logic
- `IHttpClientFactory` - HTTP clients
- `ILogger<T>` - logging
- `IOptions<T>`, `IConfiguration` - configuration
- `IMemoryCache`, `IDistributedCache` - caching

**Insight**: Moq's 23,546 file usage suggests significant interface mocking activity, but no breakdown of which interfaces are mocked most.

---

## Known High-Priority Interfaces (Expert Knowledge)

Based on framework design guidelines and common usage:

### Tier 1: Ubiquitous
- `IEnumerable<T>` / `IEnumerable`
- `IDisposable`
- `IAsyncDisposable`

### Tier 2: Collections
- `IList<T>` / `IList`
- `ICollection<T>` / `ICollection`
- `IDictionary<TKey, TValue>` / `IDictionary`
- `ISet<T>`
- `IReadOnlyList<T>`
- `IReadOnlyCollection<T>`
- `IReadOnlyDictionary<TKey, TValue>`
- `IReadOnlySet<T>`

### Tier 3: Async/Streaming
- `IAsyncEnumerable<T>`
- `IObservable<T>`
- `IObserver<T>`

### Tier 4: Comparison/Equality
- `IEquatable<T>`
- `IComparable<T>` / `IComparable`
- `IComparer<T>` / `IComparer`
- `IEqualityComparer<T>` / `IEqualityComparer`

### Tier 5: Formatting/Parsing
- `IFormattable`
- `ISpanFormattable`
- `IParsable<T>`
- `ISpanParsable<T>`
- `IConvertible`

### Tier 6: DI/ASP.NET Core
- `IServiceProvider`
- `IServiceCollection`
- `IServiceScope`
- `IServiceScopeFactory`
- `IOptions<T>`
- `IOptionsSnapshot<T>`
- `IOptionsMonitor<T>`
- `ILogger<T>`
- `ILoggerFactory`
- `IConfiguration`
- `IConfigurationSection`
- `IHostedService`
- `IHttpClientFactory`

### Tier 7: Data Access
- `IDbConnection`
- `IDbCommand`
- `IDataReader`
- `IDbTransaction`

### Tier 8: Notifications (WPF/MAUI/Blazor)
- `INotifyPropertyChanged`
- `INotifyCollectionChanged`
- `INotifyDataErrorInfo`

### Tier 9: Serialization
- `ISerializable` (legacy)
- `IXmlSerializable`

---

## Conclusions: Top 50 .NET Interfaces by Real-World Usage

Based on BigQuery analysis of GitHub's `sample_contents` dataset (10% of popular repos).

### Final Ranked List

| Rank | Interface | Impl | Usage | Total | Category |
|------|-----------|------|-------|-------|----------|
| 1 | **IEnumerable<T>/IEnumerable** | 491 | 14,863 | 15,354 | Collections |
| 2 | **IList<T>/IList** | 179 | 5,253 | 5,432 | Collections |
| 3 | **IDictionary<K,V>/IDictionary** | 127 | 3,168 | 3,295 | Collections |
| 4 | **ICollection<T>/ICollection** | 166 | 1,641 | 1,807 | Collections |
| 5 | **IDisposable** | 1,177 | 450 | 1,627 | Lifecycle |
| 6 | **IEnumerator<T>/IEnumerator** | 432 | 1,304 | 1,736 | Collections |
| 7 | **IAsyncResult** | - | 1,328 | 1,328 | Async (legacy) |
| 8 | **IEquatable<T>** | 866 | - | 866 | Comparison |
| 9 | **IReadOnlyList<T>** | 9 | 557 | 566 | Collections |
| 10 | **ICommand** | 140 | 628 | 768 | MVVM |
| 11 | **ILogger/ILogger<T>** | 68 | 697 | 765 | Logging |
| 12 | **IComparer<T>/IComparer** | 334 | 360 | 694 | Comparison |
| 13 | **IEqualityComparer<T>/IEqualityComparer** | 297 | 323 | 620 | Comparison |
| 14 | **IQueryable<T>** | - | 521 | 521 | LINQ |
| 15 | **IObservable<T>** | 13 | 495 | 508 | Reactive |
| 16 | **IServiceProvider** | 36 | 387 | 423 | DI |
| 17 | **IComparable<T>/IComparable** | 320 | - | 320 | Comparison |
| 18 | **ISet<T>/ISet** | 10 | 266 | 276 | Collections |
| 19 | **INotifyPropertyChanged** | 230 | - | 230 | Notifications |
| 20 | **IDataReader** | - | 231 | 231 | Data Access |
| 21 | **IRepository<T>** | 10 | 217 | 227 | Repository Pattern |
| 22 | **IActionResult** | - | 176 | 176 | ASP.NET MVC |
| 23 | **IDbConnection** | - | 161 | 161 | Data Access |
| 24 | **ISession** | - | 152 | 152 | ORM |
| 25 | **IDbCommand** | - | 144 | 144 | Data Access |
| 26 | **ICloneable** | 114 | - | 114 | Cloning (legacy) |
| 27 | **ISerializable** | 52 | - | 52 | Serialization (legacy) |
| 28 | **INotifyPropertyChanging** | 37 | - | 37 | Notifications |
| 29 | **IFormattable** | 26 | - | 26 | Formatting |
| 30 | **IXmlSerializable** | 24 | - | 24 | Serialization |

### Key Insights

1. **Collections dominate**: `IEnumerable`, `IList`, `IDictionary`, `ICollection` are the top 4 by usage
2. **IDisposable is #1 for implementations**: Most commonly implemented interface
3. **Usage vs Implementation differs**: `IEnumerable` is rarely implemented but heavily used as a type
4. **Comparison interfaces are popular**: `IEquatable<T>`, `IComparer<T>`, `IComparable<T>`
5. **DI/Logging matter**: `ILogger`, `IServiceProvider` are significant
6. **MVVM is alive**: `ICommand`, `INotifyPropertyChanged` still heavily used
7. **Legacy persists**: `IAsyncResult`, `ICloneable`, `ISerializable` still appear

### Recommendations for KnockOff

**Priority 1 - Built-in stub support:**
- `IDisposable` / `IAsyncDisposable`
- `IEnumerable<T>` / `IEnumerable`
- `IList<T>` / `IReadOnlyList<T>`
- `IDictionary<K,V>` / `IReadOnlyDictionary<K,V>`
- `ICollection<T>` / `IReadOnlyCollection<T>`

**Priority 2 - Common patterns:**
- `ILogger<T>` / `ILogger`
- `IServiceProvider`
- `IOptions<T>` (not in data but common in ASP.NET Core)
- `INotifyPropertyChanged`
- `ICommand`

**Priority 3 - Comparison interfaces:**
- `IEquatable<T>`
- `IComparable<T>`
- `IComparer<T>`
- `IEqualityComparer<T>`

**Not worth special handling:**
- Library-specific interfaces (ICoreAnnotation, IMessage, etc.)
- Legacy interfaces (ICloneable, ISerializable, IAsyncResult)

---

## References

- [Matt Warren - Analysing C# code on GitHub with BigQuery](https://mattwarren.org/2017/10/12/Analysing-C-code-on-GitHub-with-BigQuery/)
- [Visual Studio Magazine - Analysis Summary](https://visualstudiomagazine.com/articles/2017/10/16/code-analysis.aspx)
- [IEEE - .NET Framework and C# Features Adoption](https://ieeexplore.ieee.org/abstract/document/7424222/)
- [Microsoft Research - UP-Miner](https://www.microsoft.com/en-us/research/publication/mining-succinct-and-high-coverage-api-usage-patterns-from-source-code/)
- [Code4IT - Moq vs NSubstitute](https://www.code4it.dev/blog/moq-vs-nsubstitute-syntax/)
- [InfoWorld - Collection Interfaces Guide](https://www.infoworld.com/article/2335671/how-to-use-ienumerable-icollection-ilist-and-iqueryable-in-c-sharp.html)
