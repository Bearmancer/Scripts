/*
 * Comprehensive C# Syntax Reference with Detailed Explanations
 * 
 * This file serves as a complete reference guide to C# syntax, covering:
 * - All keywords, operators, and symbols
 * - Indentation and formatting conventions
 * - Design principles and philosophy
 * - Style guide recommendations (Microsoft conventions)
 * - Comparisons with PowerShell to highlight differences
 * 
 * C# Design Philosophy:
 * - Strongly-typed, object-oriented language
 * - Type safety enforced at compile time
 * - Based on .NET framework/runtime
 * - Multi-paradigm (OOP, functional, imperative)
 * - Compiled to intermediate language (IL), then JIT compiled
 * - Rich IDE support (IntelliSense, refactoring)
 * - Designed for enterprise application development
 * 
 * Key Differences from PowerShell:
 * - C#: Compiled, statically typed, explicit declarations
 * - PowerShell: Interpreted, dynamically typed, implicit declarations
 * - C#: PascalCase for public members, camelCase for private
 * - PowerShell: Verb-Noun PascalCase for functions, camelCase for variables
 * - C#: Requires semicolons, braces for all blocks
 * - PowerShell: Semicolons optional, braces for blocks
 * - C#: Strong type system enforced at compile time
 * - PowerShell: Dynamic typing with optional type constraints
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace SyntaxReference
{
    // ========================================================================
    // VARIABLES AND DATA TYPES
    // ========================================================================
    
    /// <summary>
    /// Demonstrates variable declaration and data types in C#.
    /// 
    /// C# Variables:
    /// - Must be declared with type (strongly typed)
    /// - Type can be explicit (string userName) or inferred (var userName)
    /// - Naming convention: camelCase for local variables, PascalCase for public
    /// 
    /// Comparison with PowerShell:
    ///     C#:          string userName = "John";
    ///     PowerShell:  $userName = "John"
    ///     
    /// C# requires type declaration, PowerShell infers type dynamically.
    /// C# is more verbose but provides compile-time type safety.
    /// </summary>
    public class VariablesAndDataTypes
    {
        public static void DemonstrateVariableDeclaration()
        {
            // EXPLICIT TYPE DECLARATION (recommended for clarity)
            string userName = "JohnDoe";                    // String type
            int userAge = 30;                               // 32-bit integer
            double accountBalance = 1234.56;                // 64-bit floating point
            bool isAccountActive = true;                    // Boolean (true/false)
            DateTime accountCreatedDate = DateTime.Now;     // Date and time
            
            // VAR KEYWORD (type inferred by compiler)
            // Type is still strongly determined at compile time
            var emailAddress = "john@example.com";          // Inferred as string
            var loginAttempts = 0;                          // Inferred as int
            var transactionAmount = 99.99;                  // Inferred as double
            var hasAdminPrivileges = false;                 // Inferred as bool
            
            // NULLABLE VALUE TYPES (can hold null)
            // Value types (int, bool, etc.) normally cannot be null
            int? nullableAge = null;                        // Nullable int
            bool? optionalFlag = null;                      // Nullable bool
            DateTime? lastLoginDate = null;                 // Nullable DateTime
            
            // NULL-COALESCING OPERATOR (??)
            int displayAge = nullableAge ?? 0;              // Use 0 if null
            
            // NULL-COALESCING ASSIGNMENT (??=) - C# 8.0+
            string? optionalName = null;
            optionalName ??= "DefaultName";                 // Assign if null
            
            // CONSTANTS (compile-time constant values)
            const string ApplicationName = "MyApp";
            const int MaximumRetries = 3;
            const double TaxRate = 0.08;
            
            // READONLY (runtime constant, set once in constructor)
            readonly string connectionString = "Server=localhost;";
        }
        
        public static void DemonstrateNumericTypes()
        {
            // INTEGER TYPES (signed)
            sbyte signedByte = -128;                        // 8-bit signed (-128 to 127)
            short shortInteger = -32768;                    // 16-bit signed
            int standardInteger = -2147483648;              // 32-bit signed (most common)
            long longInteger = -9223372036854775808L;       // 64-bit signed (L suffix)
            
            // INTEGER TYPES (unsigned)
            byte unsignedByte = 255;                        // 8-bit unsigned (0 to 255)
            ushort unsignedShort = 65535;                   // 16-bit unsigned
            uint unsignedInteger = 4294967295U;             // 32-bit unsigned (U suffix)
            ulong unsignedLong = 18446744073709551615UL;    // 64-bit unsigned (UL suffix)
            
            // FLOATING POINT TYPES
            float singlePrecision = 3.14f;                  // 32-bit (F suffix, ~7 digits)
            double doublePrecision = 3.14159265359;         // 64-bit (default, ~15 digits)
            decimal highPrecision = 3.14159265358979323846m;// 128-bit (M suffix, ~28 digits)
            
            // Decimal is best for financial calculations (no rounding errors)
            decimal price = 19.99m;
            decimal taxAmount = price * 0.08m;
            decimal totalPrice = price + taxAmount;
            
            // NUMBER FORMATTING
            int hexValue = 0xFF;                            // Hexadecimal (255)
            int binaryValue = 0b1010;                       // Binary (10) - C# 7.0+
            int withSeparators = 1_000_000;                 // Digit separators - C# 7.0+
        }
        
        public static void DemonstrateStrings()
        {
            // STRING DECLARATION (immutable reference type)
            string firstName = "John";
            string lastName = "Doe";
            
            // STRING CONCATENATION
            string fullName = firstName + " " + lastName;
            
            // STRING INTERPOLATION (C# 6.0+, most readable)
            int age = 30;
            string greeting = $"Hello, {fullName}! You are {age} years old.";
            
            // STRING INTERPOLATION WITH EXPRESSIONS
            string ageNextYear = $"Next year you will be {age + 1} years old.";
            
            // VERBATIM STRINGS (@ prefix, preserves formatting and escapes)
            string filePath = @"C:\Users\John\Documents\file.txt";
            string multiLine = @"Line 1
Line 2
Line 3";
            
            // RAW STRING LITERALS (C# 11+, no escaping needed)
            string jsonExample = """
                {
                    "name": "John",
                    "age": 30
                }
                """;
            
            // STRING METHODS
            string originalText = "  Python Programming  ";
            string lowercaseText = originalText.ToLower();
            string uppercaseText = originalText.ToUpper();
            string trimmedText = originalText.Trim();
            string replacedText = originalText.Replace("Python", "C#");
            bool containsWord = originalText.Contains("Python");
            bool startsWithSpace = originalText.StartsWith(" ");
            string substring = originalText.Substring(2, 6);    // Start index, length
            
            // STRING COMPARISON
            string password1 = "Secret123";
            string password2 = "secret123";
            
            // Case-sensitive comparison (default)
            bool exactMatch = password1 == password2;           // false
            
            // Case-insensitive comparison
            bool caseInsensitiveMatch = string.Equals(
                password1,
                password2,
                StringComparison.OrdinalIgnoreCase
            );  // true
            
            // C# is CASE-SENSITIVE by default (like Python, unlike PowerShell)
        }
        
        public static void DemonstrateCollections()
        {
            // ARRAYS (fixed size, zero-indexed)
            // Comparison with PowerShell:
            //     C#:          string[] servers = new string[3];
            //     PowerShell:  $servers = @()
            // C# requires size or initializer, PowerShell is dynamic
            
            string[] serverNames = new string[3];           // Array of 3 strings
            serverNames[0] = "WebServer01";
            serverNames[1] = "DatabaseServer01";
            serverNames[2] = "CacheServer01";
            
            // ARRAY INITIALIZATION (more concise)
            string[] servers = new string[] { "Web", "DB", "Cache" };
            int[] portNumbers = { 80, 443, 8080, 8443 };    // Type inferred
            
            // ARRAY PROPERTIES AND METHODS
            int arrayLength = servers.Length;
            int indexOf = Array.IndexOf(servers, "DB");
            Array.Sort(servers);                            // In-place sort
            Array.Reverse(servers);                         // In-place reverse
            
            // LIST<T> (dynamic size, generic collection)
            // Most commonly used collection in modern C#
            List<string> serverList = new List<string>();
            serverList.Add("WebServer01");
            serverList.Add("DatabaseServer01");
            serverList.AddRange(new[] { "Cache01", "Cache02" });
            
            // LIST INITIALIZATION (collection initializer)
            List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };
            
            // LIST METHODS
            serverList.Insert(0, "LoadBalancer");           // Insert at index
            serverList.Remove("Cache01");                   // Remove first match
            serverList.RemoveAt(0);                         // Remove at index
            bool containsDb = serverList.Contains("DatabaseServer01");
            string firstServer = serverList[0];             // Access by index
            
            // DICTIONARY<TKey, TValue> (key-value pairs, hash table)
            // Comparison with PowerShell:
            //     C#:          var dict = new Dictionary<string, int>();
            //     PowerShell:  $dict = @{}
            // PowerShell syntax is more concise
            
            Dictionary<string, string> userConfiguration = new Dictionary<string, string>
            {
                { "UserName", "JohnDoe" },
                { "EmailAddress", "john@example.com" },
                { "Department", "Engineering" }
            };
            
            // DICTIONARY INITIALIZATION (C# 6.0+)
            var serverConfig = new Dictionary<string, int>
            {
                ["WebServer"] = 80,
                ["DatabaseServer"] = 5432,
                ["CacheServer"] = 6379
            };
            
            // DICTIONARY OPERATIONS
            userConfiguration["AccessLevel"] = "Standard";  // Add or update
            string email = userConfiguration["EmailAddress"];// Access (throws if not found)
            
            // SAFE DICTIONARY ACCESS
            if (userConfiguration.TryGetValue("Department", out string department))
            {
                Console.WriteLine($"Department: {department}");
            }
            
            // DICTIONARY METHODS
            bool hasEmailKey = userConfiguration.ContainsKey("EmailAddress");
            bool hasValue = userConfiguration.ContainsValue("Engineering");
            
            // HASHSET<T> (unique elements, fast lookup)
            HashSet<string> uniqueTags = new HashSet<string>
            {
                "csharp", "programming", "dotnet"
            };
            
            uniqueTags.Add("automation");                   // Returns false if exists
            uniqueTags.Remove("dotnet");
            bool contains = uniqueTags.Contains("csharp");
            
            // SET OPERATIONS
            HashSet<int> setA = new HashSet<int> { 1, 2, 3, 4, 5 };
            HashSet<int> setB = new HashSet<int> { 4, 5, 6, 7, 8 };
            
            setA.UnionWith(setB);                           // Union (modifies setA)
            setA.IntersectWith(setB);                       // Intersection
            setA.ExceptWith(setB);                          // Difference
        }
    }
    
    // ========================================================================
    // OPERATORS
    // ========================================================================
    
    /// <summary>
    /// Demonstrates all C# operators.
    /// 
    /// Comparison with PowerShell:
    ///     Arithmetic: Same (+, -, *, /, %)
    ///     Comparison: C# uses symbols (==, !=, >, <), PowerShell uses words (-eq, -ne, -gt, -lt)
    ///     Logical: C# uses symbols (&&, ||, !), PowerShell uses words (-and, -or, -not)
    ///     
    /// C# operators are more like C/Java/Python.
    /// PowerShell uses English words for better readability.
    /// </summary>
    public class Operators
    {
        public static void DemonstrateArithmeticOperators()
        {
            // ARITHMETIC OPERATORS
            int totalItems = 10 + 5;                        // Addition: 15
            int itemsRemaining = 100 - 25;                  // Subtraction: 75
            double productPrice = 19.99 * 3;                // Multiplication: 59.97
            double averageScore = 200.0 / 4;                // Division: 50.0
            int remainderValue = 17 % 5;                    // Modulus: 2
            
            // INTEGER DIVISION (truncates decimal)
            int integerDivision = 200 / 4;                  // 50 (not 50.0)
            int truncated = 7 / 2;                          // 3 (not 3.5)
            
            // INCREMENT AND DECREMENT
            int counter = 0;
            counter++;                                      // Post-increment (use then add)
            ++counter;                                      // Pre-increment (add then use)
            counter--;                                      // Post-decrement
            --counter;                                      // Pre-decrement
            
            int value = 5;
            int postIncrement = value++;                    // postIncrement = 5, value = 6
            int preIncrement = ++value;                     // preIncrement = 7, value = 7
        }
        
        public static void DemonstrateComparisonOperators()
        {
            // COMPARISON OPERATORS
            // C# uses symbols (like Python), PowerShell uses words
            
            int currentAge = 25;
            int minimumAge = 18;
            
            bool isOldEnough = currentAge >= minimumAge;    // Greater than or equal
            bool isExactAge = currentAge == minimumAge;     // Equal to
            bool isNotEqual = currentAge != minimumAge;     // Not equal to
            bool isGreater = currentAge > minimumAge;       // Greater than
            bool isLess = currentAge < minimumAge;          // Less than
            bool isLessOrEqual = currentAge <= minimumAge;  // Less than or equal
            
            // STRING COMPARISON
            string password1 = "Secret123";
            string password2 = "secret123";
            
            // Case-sensitive by default
            bool exactMatch = password1 == password2;       // false
            
            // Case-insensitive comparison
            bool caseInsensitive = string.Equals(
                password1,
                password2,
                StringComparison.OrdinalIgnoreCase
            );  // true
        }
        
        public static void DemonstrateLogicalOperators()
        {
            // LOGICAL OPERATORS
            // Comparison:
            //     C#:          hasLicense && hasSubscription
            //     PowerShell:  $hasLicense -and $hasSubscription
            // C# is shorter with symbols
            
            bool hasValidLicense = true;
            bool hasCurrentSubscription = true;
            bool canAccessPremium = hasValidLicense && hasCurrentSubscription;  // AND
            
            bool isWeekend = false;
            bool isHoliday = false;
            bool isDayOff = isWeekend || isHoliday;         // OR
            
            bool serviceIsRunning = true;
            bool needsRestart = !serviceIsRunning;          // NOT (logical negation)
            
            // SHORT-CIRCUIT EVALUATION
            // && stops if first is false
            // || stops if first is true
            bool result = CheckCondition() && ExpensiveOperation();
        }
        
        private static bool CheckCondition() => true;
        private static bool ExpensiveOperation() => true;
        
        public static void DemonstrateBitwiseOperators()
        {
            // BITWISE OPERATORS (operate on individual bits)
            int a = 0b1100;                                 // 12 in binary
            int b = 0b1010;                                 // 10 in binary
            
            int bitwiseAnd = a & b;                         // 0b1000 (8)
            int bitwiseOr = a | b;                          // 0b1110 (14)
            int bitwiseXor = a ^ b;                         // 0b0110 (6)
            int bitwiseNot = ~a;                            // Invert all bits
            int leftShift = a << 2;                         // Shift left 2 bits (48)
            int rightShift = a >> 2;                        // Shift right 2 bits (3)
        }
        
        public static void DemonstrateAssignmentOperators()
        {
            // ASSIGNMENT OPERATORS
            int totalScore = 0;
            totalScore += 10;                               // Add and assign
            totalScore -= 5;                                // Subtract and assign
            totalScore *= 2;                                // Multiply and assign
            totalScore /= 3;                                // Divide and assign
            totalScore %= 7;                                // Modulus and assign
            
            // BITWISE ASSIGNMENT
            int flags = 0b1100;
            flags &= 0b1010;                                // Bitwise AND assign
            flags |= 0b0011;                                // Bitwise OR assign
            flags ^= 0b0101;                                // Bitwise XOR assign
            flags <<= 2;                                    // Left shift assign
            flags >>= 1;                                    // Right shift assign
        }
        
        public static void DemonstrateNullOperators()
        {
            // NULL-CONDITIONAL OPERATOR (?.) - C# 6.0+
            // Safely access members, returns null if object is null
            string? nullableString = null;
            int? length = nullableString?.Length;           // null (no exception)
            
            // NULL-COALESCING OPERATOR (??)
            // Returns left operand if not null, otherwise right operand
            string displayName = nullableString ?? "DefaultName";
            
            // NULL-COALESCING ASSIGNMENT (??=) - C# 8.0+
            // Assign right operand only if left is null
            string? optionalValue = null;
            optionalValue ??= "NewValue";                   // Assigns "NewValue"
            
            // NULL-FORGIVING OPERATOR (!) - C# 8.0+
            // Tells compiler to ignore null warning (use with caution!)
            string definitelyNotNull = nullableString!;     // Suppress warning
        }
        
        public static void DemonstrateTernaryOperator()
        {
            // CONDITIONAL (TERNARY) OPERATOR
            // Syntax: condition ? trueValue : falseValue
            
            bool isAccountActive = true;
            string statusMessage = isAccountActive ? "Active" : "Inactive";
            
            // Can be nested (but avoid for readability)
            int age = 25;
            string category = age < 18 ? "Minor" :
                            age < 65 ? "Adult" :
                            "Senior";
        }
    }
    
    // ========================================================================
    // CONTROL FLOW
    // ========================================================================
    
    /// <summary>
    /// Demonstrates control flow statements in C#.
    /// 
    /// C# uses braces {} for all blocks (unlike Python's indentation).
    /// Indentation is stylistic (4 spaces convention).
    /// 
    /// Comparison with PowerShell:
    ///     Both use braces for blocks
    ///     C# requires semicolons, PowerShell doesn't
    ///     C# has more modern features (switch expressions, pattern matching)
    /// </summary>
    public class ControlFlow
    {
        public static void DemonstrateIfStatement()
        {
            // IF-ELSE IF-ELSE STATEMENT
            // Syntax same as PowerShell, but requires semicolons
            
            double accountBalance = 150.00;
            double minimumBalance = 100.00;
            double transactionAmount = 75.00;
            
            if (accountBalance > minimumBalance + transactionAmount)
            {
                Console.WriteLine("Transaction approved. Sufficient funds.");
                accountBalance -= transactionAmount;
            }
            else if (accountBalance > transactionAmount)
            {
                Console.WriteLine("Transaction approved with warning.");
                accountBalance -= transactionAmount;
            }
            else
            {
                Console.WriteLine("Transaction declined. Insufficient funds.");
            }
            
            // SINGLE-LINE IF (braces optional for single statement)
            if (accountBalance < minimumBalance)
                Console.WriteLine("Balance below minimum");  // Not recommended style
        }
        
        public static void DemonstrateSwitchStatement()
        {
            // TRADITIONAL SWITCH STATEMENT
            DayOfWeek dayOfWeek = DateTime.Now.DayOfWeek;
            
            switch (dayOfWeek)
            {
                case DayOfWeek.Monday:
                    Console.WriteLine("Start of work week");
                    break;                                  // Required (no fall-through)
                
                case DayOfWeek.Friday:
                    Console.WriteLine("Last work day");
                    break;
                
                case DayOfWeek.Saturday:
                case DayOfWeek.Sunday:                      // Multiple cases
                    Console.WriteLine("Weekend day");
                    break;
                
                default:
                    Console.WriteLine("Midweek day");
                    break;
            }
        }
        
        public static void DemonstrateSwitchExpression()
        {
            // SWITCH EXPRESSION (C# 8.0+)
            // More concise, returns value, pattern matching
            
            DayOfWeek day = DateTime.Now.DayOfWeek;
            
            string message = day switch
            {
                DayOfWeek.Monday => "Start of work week",
                DayOfWeek.Friday => "Last work day",
                DayOfWeek.Saturday or DayOfWeek.Sunday => "Weekend",
                _ => "Midweek day"                          // Discard pattern (default)
            };
            
            // PATTERN MATCHING WITH SWITCH
            object value = 42;
            
            string description = value switch
            {
                int intValue => $"Integer: {intValue}",
                string stringValue => $"String: {stringValue}",
                double doubleValue when doubleValue > 100 => "Large number",
                null => "Null value",
                _ => "Unknown type"
            };
        }
        
        public static void DemonstrateForLoop()
        {
            // FOR LOOP (C-style, same as PowerShell)
            string[] serverNames = { "Web01", "DB01", "Cache01" };
            
            for (int serverIndex = 0; serverIndex < serverNames.Length; serverIndex++)
            {
                string currentServerName = serverNames[serverIndex];
                Console.WriteLine($"Server {serverIndex + 1}: {currentServerName}");
            }
            
            // MULTIPLE VARIABLES IN FOR LOOP
            for (int i = 0, j = 10; i < j; i++, j--)
            {
                Console.WriteLine($"i={i}, j={j}");
            }
        }
        
        public static void DemonstrateForeachLoop()
        {
            // FOREACH LOOP (iterator-based)
            // Comparison:
            //     C#:          foreach (var server in servers)
            //     PowerShell:  foreach ($server in $servers)
            // Similar syntax, C# needs var/type
            
            string[] fileExtensions = { ".txt", ".log", ".csv", ".json" };
            
            foreach (string currentExtension in fileExtensions)
            {
                Console.WriteLine($"Extension: {currentExtension}");
            }
            
            // FOREACH WITH VAR
            List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };
            
            foreach (var number in numbers)
            {
                Console.WriteLine(number);
            }
            
            // FOREACH WITH INDEX (using Select)
            foreach (var (extension, index) in fileExtensions.Select((e, i) => (e, i)))
            {
                Console.WriteLine($"[{index}] {extension}");
            }
        }
        
        public static void DemonstrateWhileLoop()
        {
            // WHILE LOOP
            int attemptCount = 0;
            int maxAttempts = 5;
            bool connectionSuccessful = false;
            
            while (!connectionSuccessful && attemptCount < maxAttempts)
            {
                attemptCount++;
                Console.WriteLine($"Attempt {attemptCount} of {maxAttempts}");
                
                // Simulate connection
                connectionSuccessful = new Random().Next(2) == 1;
                
                if (!connectionSuccessful)
                {
                    System.Threading.Thread.Sleep(2000);
                }
            }
        }
        
        public static void DemonstrateDoWhileLoop()
        {
            // DO-WHILE LOOP (executes at least once)
            int retryCount = 0;
            
            do
            {
                retryCount++;
                Console.WriteLine($"Retry attempt: {retryCount}");
            }
            while (retryCount < 3);
        }
        
        public static void DemonstrateBreakContinue()
        {
            // BREAK AND CONTINUE
            // break: Exit loop immediately
            // continue: Skip to next iteration
            
            // Process only even numbers, stop at 15
            for (int currentNumber = 1; currentNumber <= 20; currentNumber++)
            {
                if (currentNumber % 2 != 0)
                {
                    continue;                               // Skip odd numbers
                }
                
                if (currentNumber > 15)
                {
                    break;                                  // Stop after 15
                }
                
                Console.WriteLine($"Processing: {currentNumber}");
            }
        }
    }
    
    // ========================================================================
    // METHODS (FUNCTIONS)
    // ========================================================================
    
    /// <summary>
    /// Demonstrates method definitions in C#.
    /// 
    /// C# methods are always part of a class (no standalone functions).
    /// Must specify return type (or void for no return).
    /// 
    /// Comparison with PowerShell:
    ///     C#: static return type, PascalCase naming
    ///     PowerShell: function Verb-Noun naming, no return type
    ///     
    /// C# is more verbose but provides compile-time type checking.
    /// </summary>
    public class Methods
    {
        // BASIC METHOD WITH RETURN VALUE
        public static string GetUserAccountStatus(string userName, bool includeDetails = false)
        {
            /*
             * Retrieves account status for a user.
             * 
             * Parameters:
             *   userName: The username to check (required)
             *   includeDetails: Include detailed info (optional, default: false)
             * 
             * Returns:
             *   String with account status information
             */
            
            Console.WriteLine($"Checking status for: {userName}");
            
            string status = $"User: {userName}, Active: true";
            
            if (includeDetails)
            {
                status += ", Details: Extended information";
            }
            
            return status;
        }
        
        // METHOD WITH MULTIPLE PARAMETERS
        public static void CreateServerConnection(
            string serverName,
            int portNumber = 443,
            string protocol = "HTTPS",
            int timeoutSeconds = 30)
        {
            Console.WriteLine($"Connecting to {serverName}:{portNumber}");
            Console.WriteLine($"Protocol: {protocol}, Timeout: {timeoutSeconds}s");
        }
        
        // METHOD WITH OUT PARAMETER (returns multiple values)
        public static bool TryParseUserAge(string input, out int age)
        {
            /*
             * Try to parse age from string.
             * Returns true if successful, false otherwise.
             * Parsed age is returned via out parameter.
             */
            
            if (int.TryParse(input, out age))
            {
                return age >= 0 && age <= 150;
            }
            
            age = 0;
            return false;
        }
        
        // METHOD WITH REF PARAMETER (pass by reference)
        public static void IncrementCounter(ref int counter)
        {
            /*
             * Increment counter by reference.
             * Changes are visible to caller.
             */
            counter++;
        }
        
        // METHOD WITH PARAMS ARRAY (variable number of arguments)
        public static int CalculateSum(params int[] numbers)
        {
            /*
             * Calculate sum of variable number of integers.
             * Can be called with any number of arguments.
             */
            
            int totalSum = 0;
            foreach (int number in numbers)
            {
                totalSum += number;
            }
            return totalSum;
        }
        
        // Usage: CalculateSum(1, 2, 3, 4, 5)
        // Usage: CalculateSum(10, 20)
        
        // EXPRESSION-BODIED METHOD (C# 6.0+)
        // Single-expression methods with => syntax
        public static int SquareNumber(int number) => number * number;
        
        public static string FormatUserName(string firstName, string lastName) =>
            $"{lastName}, {firstName}";
        
        // LOCAL FUNCTION (C# 7.0+)
        // Functions defined inside methods
        public static int CalculateFactorial(int number)
        {
            if (number < 0)
            {
                throw new ArgumentException("Number must be non-negative");
            }
            
            return ComputeFactorial(number);
            
            // Local function (only accessible within parent method)
            int ComputeFactorial(int n)
            {
                if (n <= 1) return 1;
                return n * ComputeFactorial(n - 1);
            }
        }
        
        // METHOD OVERLOADING (same name, different parameters)
        public static void PrintMessage(string message)
        {
            Console.WriteLine(message);
        }
        
        public static void PrintMessage(string message, int repeatCount)
        {
            for (int i = 0; i < repeatCount; i++)
            {
                Console.WriteLine(message);
            }
        }
        
        public static void PrintMessage(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        
        // NAMED ARGUMENTS (call with parameter names)
        public static void DemonstrateNamedArguments()
        {
            // Can call with arguments in any order
            CreateServerConnection(
                protocol: "HTTP",
                serverName: "server01",
                timeoutSeconds: 60,
                portNumber: 8080
            );
        }
    }
    
    // ========================================================================
    // DELEGATES AND LAMBDA EXPRESSIONS
    // ========================================================================
    
    /// <summary>
    /// Demonstrates delegates (function pointers) and lambda expressions.
    /// 
    /// Delegates are type-safe function pointers.
    /// Lambda expressions are anonymous functions.
    /// 
    /// Comparison with PowerShell:
    ///     C#:          Func<int, int> square = x => x * x;
    ///     PowerShell:  $square = { param($x) $x * $x }
    ///     
    /// C# has stronger typing and more concise lambda syntax.
    /// </summary>
    public class DelegatesAndLambdas
    {
        // DELEGATE DECLARATION (function type)
        public delegate int BinaryOperation(int a, int b);
        
        public static void DemonstrateDelegates()
        {
            // DELEGATE INSTANCE
            BinaryOperation addOperation = Add;
            BinaryOperation multiplyOperation = Multiply;
            
            int sum = addOperation(10, 20);                 // 30
            int product = multiplyOperation(10, 20);        // 200
            
            // MULTICAST DELEGATE (multiple functions)
            BinaryOperation combined = Add;
            combined += Multiply;
            combined(5, 10);                                // Calls both Add and Multiply
        }
        
        private static int Add(int a, int b) => a + b;
        private static int Multiply(int a, int b) => a * b;
        
        public static void DemonstrateLambdaExpressions()
        {
            // LAMBDA EXPRESSION (anonymous function)
            // Syntax: (parameters) => expression or block
            
            // FUNC<T> (delegate that returns value)
            Func<int, int> squareNumber = x => x * x;
            Func<int, int, int> addNumbers = (a, b) => a + b;
            Func<string, int> stringLength = str => str.Length;
            
            int squared = squareNumber(5);                  // 25
            int sum = addNumbers(10, 20);                   // 30
            
            // ACTION<T> (delegate with no return value)
            Action<string> printMessage = message => Console.WriteLine(message);
            Action<int, int> printSum = (a, b) => Console.WriteLine($"Sum: {a + b}");
            
            printMessage("Hello");
            printSum(10, 20);
            
            // LAMBDA WITH MULTIPLE STATEMENTS
            Func<int, int, string> compareNumbers = (a, b) =>
            {
                if (a > b) return $"{a} is greater";
                if (a < b) return $"{a} is less";
                return "Equal";
            };
            
            // LAMBDA IN LINQ QUERIES
            List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            // Filter with Where
            var evenNumbers = numbers.Where(n => n % 2 == 0).ToList();
            
            // Transform with Select
            var squaredNumbers = numbers.Select(n => n * n).ToList();
            
            // Find first match
            int firstEven = numbers.First(n => n % 2 == 0);
            
            // Check condition
            bool allPositive = numbers.All(n => n > 0);
            bool hasEven = numbers.Any(n => n % 2 == 0);
        }
        
        public static void DemonstrateLinqQueries()
        {
            // LANGUAGE INTEGRATED QUERY (LINQ)
            // Query collections with SQL-like syntax
            
            var users = new List<User>
            {
                new User { Name = "Alice", Age = 30, Department = "Engineering" },
                new User { Name = "Bob", Age = 25, Department = "Sales" },
                new User { Name = "Charlie", Age = 35, Department = "Engineering" }
            };
            
            // QUERY SYNTAX (SQL-like)
            var engineersQuery = from user in users
                                where user.Department == "Engineering"
                                orderby user.Age
                                select user.Name;
            
            // METHOD SYNTAX (lambda-based, more common)
            var engineersMethod = users
                .Where(u => u.Department == "Engineering")
                .OrderBy(u => u.Age)
                .Select(u => u.Name)
                .ToList();
            
            // COMPLEX QUERY
            var averageAgeByDepartment = users
                .GroupBy(u => u.Department)
                .Select(g => new
                {
                    Department = g.Key,
                    AverageAge = g.Average(u => u.Age),
                    Count = g.Count()
                })
                .ToList();
        }
        
        private class User
        {
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public string Department { get; set; } = string.Empty;
        }
    }
    
    // ========================================================================
    // EXCEPTION HANDLING
    // ========================================================================
    
    /// <summary>
    /// Demonstrates exception handling in C#.
    /// 
    /// C# uses try-catch-finally (similar to PowerShell).
    /// All exceptions derive from System.Exception.
    /// 
    /// Comparison with PowerShell:
    ///     Both use try-catch-finally
    ///     C# has more specific exception types
    ///     C# requires explicit exception types in catch
    /// </summary>
    public class ExceptionHandling
    {
        public static string ReadConfigurationFile(string configFilePath)
        {
            /*
             * Read configuration file with comprehensive error handling.
             * 
             * Parameters:
             *   configFilePath: Path to configuration file
             * 
             * Returns:
             *   Configuration content or null if error
             */
            
            try
            {
                Console.WriteLine($"Reading config: {configFilePath}");
                
                // This will throw if file doesn't exist
                string configurationContent = File.ReadAllText(configFilePath);
                
                if (string.IsNullOrWhiteSpace(configurationContent))
                {
                    throw new InvalidDataException("Config file is empty");
                }
                
                Console.WriteLine("Configuration loaded successfully");
                return configurationContent;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"File not found: {ex.Message}");
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied: {ex.Message}");
                return null;
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine($"Invalid data: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Catch-all for unexpected exceptions
                Console.WriteLine($"Unexpected error: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().Name}");
                return null;
            }
            finally
            {
                // Always executes
                Console.WriteLine("Completed file read attempt");
            }
        }
        
        // THROW STATEMENT (raise exception)
        public static void SetDatabaseConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException(
                    nameof(connectionString),
                    "Connection string cannot be null or empty"
                );
            }
            
            if (!connectionString.Contains("password=", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Connection string must contain password",
                    nameof(connectionString)
                );
            }
            
            Console.WriteLine("Connection established");
        }
        
        // CUSTOM EXCEPTION
        public class InsufficientFundsException : Exception
        {
            public decimal Balance { get; }
            public decimal Amount { get; }
            public decimal Shortage => Amount - Balance;
            
            public InsufficientFundsException(decimal balance, decimal amount)
                : base($"Insufficient funds: need ${amount}, have ${balance}")
            {
                Balance = balance;
                Amount = amount;
            }
        }
        
        public static decimal WithdrawFunds(decimal accountBalance, decimal withdrawalAmount)
        {
            if (withdrawalAmount > accountBalance)
            {
                throw new InsufficientFundsException(accountBalance, withdrawalAmount);
            }
            
            return accountBalance - withdrawalAmount;
        }
        
        // EXCEPTION FILTER (C# 6.0+)
        public static void DemonstrateExceptionFilter()
        {
            try
            {
                // Some operation
                throw new InvalidOperationException("Test");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Test"))
            {
                // Only catch if condition is true
                Console.WriteLine("Caught with filter");
            }
        }
    }
    
    // Character count comparison will be in comments at end
    
    /// <summary>
    /// Main program entry point.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("C# Syntax Reference - Complete Implementation");
            Console.WriteLine("=".PadRight(70, '='));
            Console.WriteLine("This file demonstrates all C# syntax with detailed explanations");
            Console.WriteLine("Compare with PowerShell and Python syntax references");
            Console.WriteLine("=".PadRight(70, '='));
        }
    }
}

/*
 * ============================================================================
 * CHARACTER COUNT COMPARISON FOR COMMON OPERATIONS
 * ============================================================================
 * 
 * Comparing C#, PowerShell, and Python:
 * 
 * 1. VARIABLE ASSIGNMENT
 *    C#:          string userName = "John";       (28 chars)
 *    PowerShell:  $userName = "John"              (22 chars)
 *    Python:      user_name = "John"              (21 chars)
 *    Winner: Python (shortest)
 * 
 * 2. ARRAY/LIST CREATION
 *    C#:          var items = new[] {1, 2, 3};    (32 chars)
 *    PowerShell:  $items = @(1, 2, 3)             (23 chars)
 *    Python:      items = [1, 2, 3]               (20 chars)
 *    Winner: Python (shortest)
 * 
 * 3. DICTIONARY/HASH TABLE
 *    C#:          var d = new Dictionary...       (60+ chars)
 *    PowerShell:  $d = @{Name="John"}             (21 chars)
 *    Python:      d = {"Name": "John"}            (22 chars)
 *    Winner: PowerShell (shortest)
 * 
 * 4. FUNCTION DEFINITION
 *    C#:          public static void GetData()    (32 chars)
 *    PowerShell:  function Get-Data {              (20 chars)
 *    Python:      def get_data():                  (16 chars)
 *    Winner: Python (shortest)
 * 
 * 5. LOOP THROUGH ARRAY
 *    C#:          foreach (var item in array)     (31 chars)
 *    PowerShell:  foreach ($item in $array)       (29 chars)
 *    Python:      for item in array:              (21 chars)
 *    Winner: Python (shortest)
 * 
 * 6. CONDITIONAL STATEMENT
 *    C#:          if (x == 5)                     (11 chars)
 *    PowerShell:  if ($x -eq 5)                   (13 chars)
 *    Python:      if x == 5:                      (10 chars)
 *    Winner: Python (shortest)
 * 
 * OVERALL WINNER: Python (shortest for most operations)
 * 
 * C# is generally the most verbose:
 * - Requires type declarations
 * - Requires semicolons and braces
 * - More ceremonial syntax
 * 
 * BUT C# provides:
 * - Compile-time type safety
 * - Better IDE support (IntelliSense)
 * - Faster execution (compiled)
 * - Better for large-scale applications
 * 
 * ============================================================================
 * DESIGN PHILOSOPHY COMPARISON
 * ============================================================================
 * 
 * C#:
 * - Strongly-typed, object-oriented
 * - Compiled for performance
 * - Type safety enforced at compile time
 * - Designed for enterprise applications
 * - PascalCase for public, camelCase for private
 * - Rich IDE support
 * 
 * POWERSHELL:
 * - Object-oriented pipeline
 * - Verb-Noun naming for discoverability
 * - Designed for system administration
 * - Case-insensitive
 * - Dynamic typing with optional constraints
 * - Direct .NET integration
 * 
 * PYTHON:
 * - General-purpose, multi-paradigm
 * - "Batteries included" philosophy
 * - Emphasis on readability
 * - Snake_case naming convention
 * - Indentation-based syntax (enforced)
 * - Extensive third-party packages
 * 
 * ============================================================================
 * KEY SYNTACTIC DIFFERENCES
 * ============================================================================
 * 
 * VARIABLES:
 * - C#:          type variableName or var variableName
 * - PowerShell:  $variableName
 * - Python:      variable_name
 * 
 * COMMENTING:
 * - C#:          // single line, /* */ block, /// XML doc
 * - PowerShell:  # single line, <# #> block
 * - Python:      # single line, ''' ''' docstring
 * 
 * INDENTATION:
 * - C#:          4 spaces (convention, not enforced)
 * - PowerShell:  4 spaces (convention, not enforced)
 * - Python:      4 spaces (enforced by syntax)
 * 
 * BOOLEAN VALUES:
 * - C#:          true, false
 * - PowerShell:  $true, $false
 * - Python:      True, False
 * 
 * NULL/NONE:
 * - C#:          null
 * - PowerShell:  $null
 * - Python:      None
 * 
 * COMPARISON OPERATORS:
 * - C#:          ==, !=, >, <, >=, <=    (Symbols)
 * - PowerShell:  -eq, -ne, -gt, -lt     (Words)
 * - Python:      ==, !=, >, <, >=, <=    (Symbols)
 * 
 * LOGICAL OPERATORS:
 * - C#:          &&, ||, !               (Symbols)
 * - PowerShell:  -and, -or, -not        (Words)
 * - Python:      and, or, not           (Words)
 * 
 * ============================================================================
 * STYLE GUIDE SUMMARY (C# CONVENTIONS)
 * ============================================================================
 * 
 * NAMING CONVENTIONS:
 * - Classes/Methods/Properties: PascalCase (BankAccount, GetBalance)
 * - Local variables/parameters: camelCase (userName, accountBalance)
 * - Private fields: _camelCase with underscore (_accountBalance)
 * - Constants: PascalCase (MaximumRetries, DefaultTimeout)
 * - Interfaces: IPascalCase with I prefix (IRepository, IService)
 * 
 * FORMATTING:
 * - Indentation: 4 spaces (no tabs)
 * - Line length: No strict limit (aim for readability)
 * - Braces: Allman style (opening brace on new line)
 * - Operators: Spaces around operators (x = 5, not x=5)
 * - Semicolons: Required at end of statements
 * 
 * COMMENTS:
 * - XML documentation (///) for public APIs
 * - Single-line comments (//) for inline explanations
 * - Block comments (/* */) for multi-line
 * - Document "why" not "what"
 * 
 * BEST PRACTICES:
 * - Use var when type is obvious
 * - Use explicit types when clarity matters
 * - Prefer LINQ over loops when readable
 * - Use async/await for asynchronous operations
 * - Follow SOLID principles
 * - Keep methods small and focused
 * - Use meaningful variable names
 * - Handle exceptions appropriately
 * 
 * ============================================================================
 * FINAL NOTES
 * ============================================================================
 * 
 * This comprehensive reference covers C# syntax from basics to advanced topics.
 * All examples use semantically meaningful variable names for clarity.
 * 
 * C# excels at:
 * - Enterprise application development
 * - Type-safe programming
 * - High-performance applications
 * - Cross-platform development (.NET Core/.NET 5+)
 * - Rich IDE tooling support
 * 
 * Key takeaways:
 * 1. C# is strongly typed with compile-time safety
 * 2. More verbose than Python/PowerShell but safer
 * 3. Rich object-oriented features
 * 4. Excellent for large-scale applications
 * 5. Modern features (LINQ, async/await, pattern matching)
 * 
 * For expanding this code:
 * - Variable names clearly indicate purpose
 * - Methods follow single responsibility
 * - Comments explain "why" not "what"
 * - Modular structure supports enhancement
 * - Type safety prevents runtime errors
 */
