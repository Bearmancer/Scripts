# Description

-----------------------------

# JDK Modernization Patterns for Leetcode Solutions

A collated reference of JDK features that reduce boilerplate in Leetcode-style Java solutions. Use these patterns when
suggesting terseness-only refactors (never logical changes).

## HashMap Null-Check Reduction

### Pattern 1: `getOrDefault()` — Replace manual null-check + default

**Old pattern:**

```java
Integer count = map.get(key);
if (count == null) {
    count = 0;
}
count++;
map.put(key, count);
```

**Modern:**

```java
map.put(key, map.getOrDefault(key, 0) + 1);
```

**Works for:** Frequency counting, summing, any accumulate-over-map pattern.

### Pattern 2: `computeIfAbsent()` — Replace "if absent, create and put"

**Old pattern:**

```java
List<String> list = map.get(key);
if (list == null) {
    list = new ArrayList<>();
    map.put(key, list);
}
```

**Modern:**

```java
List<String> list = map.computeIfAbsent(key, k -> new ArrayList<>());
```

**Works for:** Building adjacency lists, grouping items by category, lazy initialization.

### Pattern 3: `merge()` — Replace get + compute + put with atomic merge

**Old pattern:**

```java
Integer existing = map.get(key);
if (existing == null) {
    map.put(key, 1);
} else {
    map.put(key, existing + 1);
}
```

**Modern:**

```java
map.merge(key, 1, Integer::sum);
```

**Works for:** Counting, summing, any "replace if present, insert if absent" pattern.

### Pattern 4: `compute()` — Replace conditional get+put with unified compute

**Old pattern:**

```java
Integer val = map.get(key);
if (val == null) {
    map.put(key, 1);
} else if (val == 2) {
    map.remove(key);
} else {
    map.put(key, val + 1);
}
```

**Modern:**

```java
map.compute(key, (k, v) -> v == null ? 1 : v == 2 ? null : v + 1);
```

**Works for:** Complex conditional updates that depend on current value.

### Pattern 5: `putIfAbsent()` — Conditional put only if key missing

**Old pattern:**

```java
if (!map.containsKey(key)) {
    map.put(key, value);
}
```

**Modern:**

```java
map.putIfAbsent(key, value);
```

**Works for:** Singleton initialization, deduplication.

## Switch Expression Modernization

### Old switch statement:

```java
switch (direction) {
    case "NORTH":
        return new int[]{1, 0};
    case "SOUTH":
        return new int[]{-1, 0};
    default:
        return new int[]{0, 0};
}
```

### Modern switch expression:

```java
return switch (direction) {
    case "NORTH" -> new int[]{1, 0};
    case "SOUTH" -> new int[]{-1, 0};
    default -> new int[]{0, 0};
};
```

### With `yield` for multi-statement cases:

```java
return switch (value) {
    case 1, 2 -> groupA();
    case 3 -> {
        int tmp = compute(value);
        yield tmp * 2;
    }
    default -> throw new IllegalStateException("Unexpected: " + value);
};
```

## Objects Helper Methods

### `Objects.requireNonNullElse(obj, defaultVal)` — Replace ternary null check

**Old pattern:**

```java
String result = (nullable == null) ? "default" : nullable;
```

**Modern:**

```java
String result = Objects.requireNonNullElse(nullable, "default");
```

### `Objects.toString(obj, nullDefault)` — Replace toString null guard

**Old pattern:**

```java
String s = (obj == null) ? "N/A" : obj.toString();
```

**Modern:**

```java
String s = Objects.toString(obj, "N/A");
```

### `Objects.equals(a, b)` — Replace safe null-aware equality

**Old pattern:**

```java
(a == null) ? (b == null) : a.equals(b)
```

**Modern:**

```java
Objects.equals(a, b)
```

## Collection Factory Methods

### `List.of(...)` — Replace verbose ArrayList initialization

**Old pattern:**

```java
List<String> list = new ArrayList<>();
list.add("a");
list.add("b");
list.add("c");
```

**Modern:**

```java
List<String> list = List.of("a", "b", "c");
```

### `Set.of(...)` — Replace verbose HashSet initialization

**Old pattern:**

```java
Set<Integer> set = new HashSet<>();
set.add(1);
set.add(2);
```

**Modern:**

```java
Set<Integer> set = Set.of(1, 2);
```

### `Map.of(...)` — Replace verbose HashMap initialization (up to 10 entries)

**Old pattern:**

```java
Map<String, Integer> map = new HashMap<>();
map.put("a", 1);
map.put("b", 2);
```

**Modern:**

```java
Map<String, Integer> map = Map.of("a", 1, "b", 2);
```

## Stream API Shortcuts

### Replace manual counting loop:

```java
// Old:
long count = 0;
for (int x : arr) if (x > 0) count++;

// Modern:
long count = Arrays.stream(arr).filter(x -> x > 0).count();
```

### Replace manual max/min search:

```java
// Old:
int max = Integer.MIN_VALUE;
for (int x : arr) if (x > max) max = x;

// Modern:
int max = Arrays.stream(arr).max().orElseThrow();
```

## Key Reminders

1. These are **terseness-only** suggestions — never change algorithm or logic
2. `List.of()`/`Set.of()`/`Map.of()` return **immutable** collections — only use when caller doesn't mutate
3. `Map.merge(key, 1, Integer::sum)` is the cleanest count-accumulate pattern
4. `computeIfAbsent` is ideal for adjacency list construction in graph problems
5. Always let the user decide whether to adopt the refactor

### Merged from \[Guide\] JDK Modernization Patterns — Map API (LeetCode Context)

# JDK Modernization Patterns — Map API (LeetCode Context)

A comprehensive reference for the five atomic conditional Map methods introduced in JDK 8.\
Each pattern is shown with a "before" (verbose) and "after" (modern) LeetCode-appropriate example.

## §1: computeIfAbsent — Lazy initialization, then operate on result

### Signature

`V computeIfAbsent(K key, Function<? super K, ? extends V> mappingFunction)`

**Returns:** The existing value if key is present (mappingFunction NOT called), or the newly computed value which is
also inserted.

**Per-line semantics:**

1. JVM checks `containsKey(key)` internally
2. If key is present → returns the existing value immediately (function never runs)
3. If key is absent → calls `mappingFunction.apply(key)`, inserts result, returns result
4. The returned value is the **same reference** you can chain calls on

### Before (Q_0049 Group Anagrams — manual containsKey guard):

```java
if (hashMap.containsKey(key)) {
    hashMap.get(key).add(str);
    continue;
}
ArrayList<String> strings = new ArrayList<>();
strings.add(str);
hashMap.put(key, strings);
```

**Line-by-line explanation of what's wrong:**

* Line 1–3: "If map already has this key, retrieve its list and add the string" — but `containsKey` + `get` is TWO
  lookups
* Line 4: `continue` — skip the creation path (puts the reader into "mental branch tracking")
* Lines 5–7: Create a new ArrayList, add the string, put it into the map — this is the "absent" path
* Total: **8 lines, 3 map operations, 2 branches**

### After with computeIfAbsent:

```java
hashMap.computeIfAbsent(key, k -> new ArrayList<>()).add(str);
```

**Line-by-line breakdown of this one-liner:**

1. `hashMap.computeIfAbsent(key, ...)` — JVM does ONE hash lookup. If key already exists, returns its list. If absent,
   creates a new ArrayList via the lambda, inserts it, and returns it.
2. `(k -> new ArrayList<>())` — This is a lambda (mapping function). The parameter `k` receives the key (but we don't
   use it here — we always want a fresh list). It only runs if the key is absent.
3. `.add(str)` — Method call chained on the returned List reference. Whether the list was existing or newly created, we
   add the string to it.

* Total: **1 line, 1 map operation, no branches**

### Example: Two-liner pattern (when returning void)

When the result of operating on the value returns void (like `addFirst`), you CANNOT chain, so you use a two-liner:

```java
LinkedList list = frequencyTracker.computeIfAbsent(frequency, k -> new LinkedList());
list.addFirst(node);
```

Here `computeIfAbsent` returns the LinkedList, we store it in a local variable, then call `addFirst` on it. `addFirst`
returns void, so chaining is impossible.

### Key insight

`computeIfAbsent` is for **"get or create, then do something with it"**. The return value is what you operate on.

## §2: putIfAbsent — Pre-computed value, returns previous

### Signature

`V putIfAbsent(K key, V value)`

**Returns:** The previous value if key was already present, or null if insertion occurred.

### Before:

```java
if (!map.containsKey(key)) {
    map.put(key, value);
}
```

### After:

```java
map.putIfAbsent(key, value);
```

**BUT** after calling `putIfAbsent`, you still need another `map.get(key)` to retrieve the value. That's why
`computeIfAbsent` is usually better for LeetCode patterns.

### computeIfAbsent vs putIfAbsent decision tree

```
Do you already have the value pre-computed?
├── Yes → putIfAbsent(key, value)
└── No, need to create it → computeIfAbsent(key, k -> createValue())

Do you need to operate on the value after?
├── Yes → computeIfAbsent (returns the value for chaining)
└── No, just want insertion → either works
```

## §3: merge — Accumulate values atomically

### Signature

`V merge(K key, V value, BiFunction<? super V, ? super V, ? extends V> remappingFunction)`

**Returns:** The new value after merge (possibly null if remapping returns null).

**Semantics:**

1. If key is absent → inserts the `value` parameter directly
2. If key is present → calls `remappingFunction(oldValue, value)` and stores the result
3. If remappingFunction returns null → removes the key

### Example 1: Char frequency count (Q_0242 Valid Anagram)

**Before:**

```java
charHashMap.put(c, charHashMap.getOrDefault(c, 0) + 1);
```

This works, but involves: getOrDefault (read) + arithmetic + put (write) = 3 operations total.

**After:**

```java
charHashMap.merge(c, 1, Integer::sum);
```

1. `c` — the key (character)
2. `1` — the default value if c is absent
3. `Integer::sum` — method reference for `(old, new) -> old + new`. If c is present, adds 1 to existing count. If
   absent, inserts 1.\
   This is a SINGLE atomic map operation.

### Example 2: Word frequency in a sentence

**Before:**

```java
for (String word : words) {
    if (freq.containsKey(word)) {
        freq.put(word, freq.get(word) + 1);
    } else {
        freq.put(word, 1);
    }
}
```

**After:**

```java
for (String word : words) {
    freq.merge(word, 1, Integer::sum);
}
```

### Example 3: Count with capped max (remapping returns null to remove)

```java
// Before:
map.put(key, Math.min(map.getOrDefault(key, 0) + 1, MAX));

// After:
map.merge(key, 1, (old, val) -> old + val > MAX ? null : old + val);
```

(But note: returning null from merge REMOVES the key.)

## §4: compute — General conditional transform

### Signature

`V compute(K key, BiFunction<? super K, ? super V, ? extends V> remappingFunction)`

**Returns:** The new value. If remapping returns null, the key is removed.

**Semantics:**\
The remappingFunction receives BOTH the key and the current value (which may be null if absent).\
This is the most GENERAL method. Use the more specific methods when possible.

### Example: Compute map where value = 2 means "remove"

```java
// Before:
Integer val = map.get(key);
if (val == null) {
    map.put(key, 1);
} else if (val == 2) {
    map.remove(key);
} else {
    map.put(key, val + 1);
}

// After:
map.compute(key, (k, v) -> v == null ? 1 : v == 2 ? null : v + 1);
```

### Decision tree for all 5 methods

```
What do you need to do with the map?
│
├─ "Get-or-create, then DO something" → computeIfAbsent ✅
│
├─ "Count / accumulate a value" → merge ✅
│
├─ "Insert if absent (value ready)" → putIfAbsent
│
├─ "Transform existing, remove if null" → compute
│
└─ "Update only if present" → computeIfPresent
```

## §5: computeIfPresent — Update only existing entries

### Signature

`V computeIfPresent(K key, BiFunction<? super K, ? super V, ? extends V> remappingFunction)`

**Returns:** The new value if updated, or null if key was absent or remapping returned null.

**Use case:** Only update a mapping if it already exists (do nothing if absent).

### Example: Increment a counter only if it exists

```java
// Before:
if (map.containsKey(key)) {
    map.put(key, map.get(key) + 1);
}

// After:
map.computeIfPresent(key, (k, v) -> v + 1);
```

### Merged from \[Guide\] JDK Modernization Patterns — Streams & Collections (LeetCode Context)

# JDK Modernization Patterns — Streams & Collections (LeetCode Context)

## §1: Arrays.stream() vs Stream.of()

### Key distinction

* `Arrays.stream(arr)` on `int[]` → returns **IntStream** (primitive, no boxing)
* `Stream.of(arr)` on `int[]` → returns `Stream<int[]>` (single-element stream of the array object — WRONG)

### Practical use cases

**Sum of array:**

```java
// Before:
int sum = 0; for (int n : nums) sum += n;

// After:
int sum = Arrays.stream(nums).sum();
```

**Max of array:**

```java
// Before:
int max = Integer.MIN_VALUE; for (int n : nums) if (n > max) max = n;

// After:
int max = Arrays.stream(nums).max().orElseThrow();
```

**Count matching condition:**

```java
// Before:
int count = 0; for (int n : nums) if (n % 2 == 0) count++;

// After:
long count = Arrays.stream(nums).filter(n -> n % 2 == 0).count();
```

**Map to new array (Q_0033 style):**

```java
int[] squared = Arrays.stream(nums).map(n -> n * n).toArray();
```

**Sub-range stream (filter by index):**

```java
Arrays.stream(arr, start, end) // Creates stream from arr[start..end-1]
```

## §2: IntStream.range() — Replacing for-loops

### Before:

```java
int[] result = new int[n];
for (int i = 0; i < n; i++) {
    result[i] = i * i;
}
```

### After:

```java
int[] result = IntStream.range(0, n).map(i -> i * i).toArray();
```

### Closed range:

```java
IntStream.rangeClosed(0, n)  // includes n, unlike range which is exclusive
```

## §3: Collectors for grouping

### Collectors.groupingBy (Q_0049 Group Anagrams)

```java
// Before:
HashMap<String, List<String>> map = new HashMap<>();
for (String str : strs) {
    String key = getKey(str);
    map.computeIfAbsent(key, k -> new ArrayList<>()).add(str);
}
return new ArrayList<>(map.values());

// After:
return new ArrayList<>(Arrays.stream(strs)
    .collect(Collectors.groupingBy(this::getKey))
    .values());
```

**But:** `groupingBy` does NOT guarantee List implementation type. For LeetCode, the `computeIfAbsent` approach is often
more explicit and equally concise.

### Collectors.toMap

```java
// Before:
HashMap<String, Integer> map = new HashMap<>();
for (String s : list) map.put(s, s.length());

// After:
Map<String, Integer> map = list.stream()
    .collect(Collectors.toMap(s -> s, String::length));
```

**⚠️ Duplicate key handling:** `toMap` throws on duplicates by default. Use the 3-argument overload:

```java
.collect(Collectors.toMap(s -> s, s -> 1, Integer::sum))
```

### Collectors.counting()

```java
Map<String, Long> freq = words.stream()
    .collect(Collectors.groupingBy(w -> w, Collectors.counting()));
```

But `merge` is often more direct for frequency counting.

## §4: Reduction patterns

### XOR for Single Number (Q_0136)

```java
// Before:
int result = 0; for (int n : nums) result ^= n;

// After:
int result = Arrays.stream(nums).reduce(0, (a, b) -> a ^ b);
```

### Multiple reductions in one pass:

Not natively supported without a custom collector.

## §5: Collection factory methods (JDK 9+)

### List.of() — Immutable, up to 10 elements

```java
// Before:
List<String> items = new ArrayList<>();
items.add("a"); items.add("b"); items.add("c");

// After:
List<String> items = List.of("a", "b", "c");
```

**⚠️ Immutable:** `items.add("d")` throws `UnsupportedOperationException`. Wrap if mutation needed:

```java
var mutable = new ArrayList<>(List.of("a", "b", "c"));
```

### Set.of()

```java
// Before:
Set<Character> opening = new HashSet<>();
opening.add('('); opening.add('{'); opening.add('[');

// After:
Set<Character> opening = Set.of('(', '{', '[');
```

Useful for O(1) membership checks in small fixed sets.

### Map.of() (up to 10 entries)

```java
// Bracket matching (Q_0020):
Map<Character, Character> pairs = Map.of(
    ')', '(',
    '}', '{',
    ']', '['
);
```

This replaces a 3-way boolean OR chain with a single map lookup.

## §6: Objects helper methods

### Objects.equals() — Null-safe equality

```java
// Before: (a == null ? b == null : a.equals(b))

// After:
Objects.equals(a, b)
```

Clean way to compare two possibly-null values.

### Objects.requireNonNullElse() — Null fallback

```java
// Before:
String name = (nullable == null) ? "default" : nullable;

// After:
String name = Objects.requireNonNullElse(nullable, "default");
```

### Objects.toString() — Null-safe toString with default

```java
// Before:
String s = (obj == null) ? "N/A" : obj.toString();

// After:
String s = Objects.toString(obj, "N/A");
```

### Objects.isNull() / nonNull() — Method references for streams

```java
// Before:
list.stream().filter(x -> x != null).collect(...)

// After:
list.stream().filter(Objects::nonNull).collect(...)  // cleaner method reference
```

## §7: Optional — When (not) to use in algorithms

### ❌ Bad — Wrapping a nullable return in hot loop

```java
// Bad:
Optional.ofNullable(map.get(key)).orElse(0);

// Better (no boxing):
map.getOrDefault(key, 0);
```

Optional adds an allocation. In LeetCode, prefer `getOrDefault` or ternaries.

### ✅ Good — When the calling code must handle absence

```java
// Before: returning null and hoping caller checks
public Integer findMax() { ... return isEmpty() ? null : max; }

// After: Optional forces caller awareness
public Optional<Integer> findMax() { ... return isEmpty() ? Optional.empty() : Optional.of(max); }
```

Not common in LeetCode method signatures.

## §8: Rule of thumb — Streams in LeetCode

**USE streams when:**

* The operation is a simple map/filter/reduce on an array
* You're collecting into a grouping or partitioning structure
* You need an IntStream for numeric reduction operations

**SKIP streams when:**

* The loop has early termination (break/return mid-loop)
* You need to track indices carefully
* Performance is critical (streams have overhead vs raw loops)
* The logic involves side effects or mutable accumulators beyond simple reduction

## §9: Complete decision table

| Old Pattern                          | New Pattern                           | Package Example       |
| ------------------------------------ | ------------------------------------- | --------------------- |
| `containsKey` + `get` + `put` (list) | `computeIfAbsent` + `.add()`          | Q_0049 Group Anagrams |
| `getOrDefault` + `put` (count)       | `merge(key, 1, Integer::sum)`         | Q_0242 Valid Anagram  |
| `putIfAbsent` + `get`                | `computeIfAbsent` (returns the value) | Q_0460 LFU Cache      |
| `if/else if (operator)`              | `switch (token) { case "+" -> ... }`  | Q_0150 RPN            |
| `if (a==x                            |                                       | a==y                  |  | a==z)` | `Set.of(x,y,z).contains(a)` | Q_0020 Valid Parentheses |
| Manual sum/loop                      | `Arrays.stream(arr).sum()`            | Any array sum         |
| Manual frequency array               | `Map.merge`                           | Any frequency counter |
| Manual loop to build list            | `Arrays.stream(arr).map(f).toList()`  | Any transform         |

### Merged from \[Guide\] JavaDoc Population Prompt — LeetCode Solutions

# JavaDoc Population Prompt — LeetCode Solutions

## Purpose

Use this prompt template when populating JavaDoc for newly added LeetCode solution files.\
It produces consistent, template-compliant documentation matching the existing 17 packages.

## Step 1: Identify the new file

* Confirm it has a `package` declaration
* Confirm it's part of the "Implemented Packages" list in CHANGELOG.md

## Step 2: Apply the class-level JavaDoc template

```text
/**
 * LeetCode #NNN: Problem Name
 * https://leetcode.com/problems/problem-name/
 *
 * <p>Brief problem summary — one or two sentences describing what the solution does.</p>
 *
 * <p><b>Implementation notes.</b></p>
 * <hr>
 * <p><b>Design choices.</b> Explanation of the algorithmic approach, key data structures used,
 * why this approach was chosen over alternatives.</p>
 * <p><b>Pros.</b></p>
 * <ul>
 *   <li>Key advantage 1 (e.g., "O(n) time")</li>
 *   <li>Key advantage 2 (e.g., "No auxiliary data structures")</li>
 * </ul>
 * <p><b>Cons.</b></p>
 * <ul>
 *   <li>Key trade-off 1 (e.g., "Mutates input array")</li>
 * </ul>
 * <p><b>Running time.</b> O(f(n)) — explanation of the bound.</p>
 * <p><b>Space usage.</b> O(f(n)) — explanation of auxiliary space.</p>
 */
```

## Step 3: Hard rules

| Rule                                      | Why                                               |
| ----------------------------------------- | ------------------------------------------------- |
| Plain `https://` URL on the second line   | Not `@see <a href="...">text</a>`                 |
| Use `<b>label.</b>` not `<h3>`/`<h4>`     | Doclint compatibility, consistent look            |
| `<hr>` after Implementation notes section | Visual separator in rendered doc                  |
| Always include Pros **and** Cons          | Even if cons are minor, shows trade-off awareness |
| Running time and Space usage at the end   | Allows quick reader scanning                      |

## Step 4: Method-level JavaDoc (when needed)

For public methods or non-trivial private helpers:

```text
/**
 * Brief one-line description.
 *
 * <p><b>Algorithm.</b> Explanation of what this method does and how.</p>
 *
 * <p><b>Complexity.</b> O(f(n)) time, O(f(n)) space.</p>
 *
 * @param paramName description
 * @return description
 */
```

Do NOT add method JavaDoc for trivial getters/delegates (methods that just call another method).

## Step 5: Verification

After populating, verify:

- [ ] Problem URL is a plain `https://` link, second line of JavaDoc
- [ ] No `<h2>`/`<h3>`/`<h4>` or `<h5>` tags anywhere
- [ ] No `@see <a href="...">` pattern
- [ ] All placeholder text replaced (no "Problem summary: (short)")
- [ ] Both Pros and Cons listed
- [ ] Running time and Space usage present
- [ ] `<hr>` present between implementation-notes intro and design-choices

## Step 6: Adding to CHANGELOG.md

After JavaDoc is populated, add an entry under `## Unreleased` and update the\
`## Implemented Packages` counter. Also add the directory to `.clineignore`.
