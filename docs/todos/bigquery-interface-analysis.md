# BigQuery Analysis: Most Used .NET Interfaces

**Status**: ✅ Complete (January 2026)

> **Note**: The `fh-bigquery.github_extracts.contents_net_cs` dataset from 2017 is no longer accessible. We used the official `bigquery-public-data.github_repos.sample_contents` dataset instead.

## Setup Instructions

### 1. Access BigQuery Console
1. Go to https://console.cloud.google.com/bigquery
2. Sign in with a Google account
3. Create a project (or select existing one)
4. No billing required for free tier (1TB/month)

### 2. Run Queries
- Paste each query into the editor
- Click "Run" to execute
- Results appear below

---

## Queries That Worked

### Dataset Schema

The `sample_contents` table has these columns:
- `id` (STRING)
- `size` (INT64)
- `content` (STRING)
- `binary` (BOOL)
- `copies` (INT64)
- `sample_repo_name` (STRING)
- `sample_ref` (STRING)
- `sample_path` (STRING) ← Use this for file filtering
- `sample_mode` (INT64)
- `sample_symlink_target` (STRING)

---

### Query 1: Non-Generic Interface Implementations

Find classes/structs implementing interfaces (`: IFoo` pattern):

```sql
SELECT
  REGEXP_EXTRACT(line, r':\s*(I[A-Z][a-zA-Z0-9]+)') AS interface_name,
  COUNT(*) AS implementation_count
FROM `bigquery-public-data.github_repos.sample_contents`,
UNNEST(SPLIT(content, '\n')) AS line
WHERE
  sample_path LIKE '%.cs'
  AND (REGEXP_CONTAINS(line, r'class\s+\w+.*:\s*I[A-Z]')
       OR REGEXP_CONTAINS(line, r'struct\s+\w+.*:\s*I[A-Z]'))
GROUP BY interface_name
HAVING interface_name IS NOT NULL
ORDER BY implementation_count DESC
LIMIT 100
```

---

### Query 2: Generic Interface Implementations

Find generic interfaces like `IList<T>`, `IEnumerable<T>`:

```sql
SELECT
  REGEXP_EXTRACT(line, r':\s*(I[A-Z][a-zA-Z0-9]+)<') AS interface_name,
  COUNT(*) AS implementation_count
FROM `bigquery-public-data.github_repos.sample_contents`,
UNNEST(SPLIT(content, '\n')) AS line
WHERE
  sample_path LIKE '%.cs'
  AND REGEXP_CONTAINS(line, r'(?:class|struct|record)\s+\w+.*:\s*I[A-Z][a-zA-Z0-9]+<')
GROUP BY interface_name
HAVING interface_name IS NOT NULL
ORDER BY implementation_count DESC
LIMIT 50
```

---

### Query 3: Interface Usage as Types (Fields, Parameters, Returns)

```sql
SELECT
  REGEXP_EXTRACT(line, r'(I[A-Z][a-zA-Z0-9]+)(?:<[^>]*>)?\s+\w+') AS interface_name,
  COUNT(*) AS usage_count
FROM `bigquery-public-data.github_repos.sample_contents`,
UNNEST(SPLIT(content, '\n')) AS line
WHERE
  sample_path LIKE '%.cs'
  AND REGEXP_CONTAINS(line, r'(?:private|public|protected|internal|readonly)\s+.*I[A-Z][a-zA-Z0-9]+(?:<[^>]*>)?\s+\w+')
GROUP BY interface_name
HAVING interface_name IS NOT NULL
ORDER BY usage_count DESC
LIMIT 50
```

---

## Results Summary

See `common-dotnet-interfaces-research.md` for the full analyzed results.

### Top 10 by Implementation Count
1. IDisposable (1,177)
2. IEquatable (434 + 432 generic)
3. IValueConverter (330)
4. IEnumerable (280 + 211 generic)
5. IEnumerator (242 + 190 generic)
6. INotifyPropertyChanged (230)
7. IComparable (211 + 109 generic)
8. IComparer (201 + 133 generic)
9. IEqualityComparer (157 + 140 generic)
10. ICommand (140)

### Top 10 by Usage as Type
1. IEnumerable (14,863)
2. IList (5,253)
3. IDictionary (3,168)
4. ICollection (1,641)
5. IAsyncResult (1,328)
6. IEnumerator (1,304)
7. IContainer (1,151)
8. ILog (886)
9. IFormatProvider (727)
10. ILogger (697)
