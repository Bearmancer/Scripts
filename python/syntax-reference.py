"""
Comprehensive Python Syntax Reference with Detailed Explanations

This module serves as a complete reference guide to Python syntax, covering:
- All keywords, operators, and symbols
- Indentation and formatting conventions (PEP 8)
- Design principles and philosophy (PEP 20 - Zen of Python)
- Style guide recommendations
- Comparisons with PowerShell to highlight differences

Python Design Philosophy (Zen of Python - PEP 20):
- Beautiful is better than ugly
- Explicit is better than implicit
- Simple is better than complex
- Readability counts
- There should be one-- and preferably only one --obvious way to do it
- Indentation is part of the syntax (not optional)
- Dynamic typing with strong type checking
- "Batteries included" standard library

Key Differences from PowerShell:
- Python: Indentation-based syntax (no braces for blocks)
- PowerShell: Brace-based syntax with optional indentation
- Python: Snake_case naming convention
- PowerShell: Verb-Noun PascalCase convention
- Python: No variable sigils (just variable_name)
- PowerShell: Requires $ sigil ($variable_name)
- Python: Text-based pipeline (stdin/stdout)
- PowerShell: Object-based pipeline (.NET objects)
"""

# ============================================================================
# VARIABLES AND DATA TYPES
# ============================================================================
"""
VARIABLE DECLARATION AND NAMING

Python variables:
- No declaration keyword needed (unlike PowerShell's $)
- Dynamically typed (type inferred from value)
- Can be type-hinted (Python 3.5+) for clarity
- Snake_case naming convention (PEP 8)

Comparison with PowerShell:
    Python:      user_name = "John"
    PowerShell:  $userName = "John"
    
Python is shorter (no $ sigil) but uses underscores instead of camelCase.
"""

# BASIC VARIABLE DECLARATION (no type hint)
user_name = "JohnDoe"                           # String type inferred
user_age = 30                                   # Integer type inferred
account_balance = 1234.56                       # Float type inferred
is_account_active = True                        # Boolean type (note: capital T)

# VARIABLE WITH TYPE HINTS (Python 3.5+)
# Type hints are optional, not enforced at runtime (use mypy for checking)
email_address: str = "john@example.com"
login_attempts: int = 0
transaction_amount: float = 99.99
has_admin_privileges: bool = False

# MULTIPLE ASSIGNMENT (tuple unpacking)
first_name, last_name, user_id = "John", "Doe", 12345

# SWAP VARIABLES (elegant Python idiom)
value_a, value_b = 10, 20
value_a, value_b = value_b, value_a             # Swap in one line

"""
BUILT-IN DATA TYPES

Python has several built-in types:
- Numeric: int, float, complex
- Sequence: list, tuple, range
- Text: str
- Binary: bytes, bytearray, memoryview
- Set: set, frozenset
- Mapping: dict
- Boolean: bool (subclass of int)
- None: NoneType
"""

# NUMBERS
integer_value = 42                              # Integer (unlimited precision)
floating_point = 3.14159                        # Float (64-bit)
complex_number = 3 + 4j                         # Complex number
hexadecimal_value = 0xFF                        # Hex notation (255)
binary_value = 0b1010                           # Binary notation (10)
scientific_notation = 1.5e10                    # 15000000000.0

# STRINGS (immutable sequence of Unicode characters)
single_quoted = 'Hello'
double_quoted = "World"                         # Both are equivalent in Python
triple_quoted = """Multi-line string
can span multiple lines
and preserves formatting"""

# STRING METHODS (Python strings are objects with many methods)
original_text = "  Python Programming  "
lowercase_text = original_text.lower()          # "  python programming  "
uppercase_text = original_text.upper()          # "  PYTHON PROGRAMMING  "
trimmed_text = original_text.strip()            # "Python Programming"
replaced_text = original_text.replace("Python", "Java")

# STRING FORMATTING (multiple approaches)
name = "Alice"
age = 30

# Old style (% formatting)
old_format = "Name: %s, Age: %d" % (name, age)

# str.format() method
format_method = "Name: {}, Age: {}".format(name, age)

# f-strings (formatted string literals - Python 3.6+, most concise)
f_string = f"Name: {name}, Age: {age}"          # Recommended modern approach
f_expression = f"Next year: {age + 1}"          # Can contain expressions

"""
LISTS (ordered, mutable sequence)

Comparison with PowerShell arrays:
    Python:      server_names = ["Web", "DB", "Cache"]
    PowerShell:  $serverNames = @("Web", "DB", "Cache")
    
Python is 4 characters shorter (no $ and @)
"""

server_names = ["WebServer01", "WebServer02", "DatabaseServer01"]
port_numbers = [80, 443, 8080, 8443]
mixed_type_list = ["text", 123, True, 3.14]    # Can hold different types

# LIST OPERATIONS
server_names.append("CacheServer01")            # Add to end
server_names.insert(0, "LoadBalancer")          # Insert at index
first_server = server_names[0]                  # Access by index (0-based)
last_server = server_names[-1]                  # Negative index from end
server_slice = server_names[1:3]                # Slice [start:end) - excludes end
reversed_servers = server_names[::-1]           # Reverse using slice

# LIST COMPREHENSION (concise way to create lists)
even_numbers = [num for num in range(20) if num % 2 == 0]
squared_values = [x**2 for x in range(10)]

"""
TUPLES (ordered, immutable sequence)

Tuples are like lists but cannot be modified after creation.
Use for fixed collections and as dictionary keys.
"""

coordinates = (10.5, 20.3, 30.1)                # Tuple with parentheses
rgb_color = 255, 128, 0                         # Parentheses optional
single_element = (42,)                          # Trailing comma for single element

# TUPLE UNPACKING
x_coordinate, y_coordinate, z_coordinate = coordinates

"""
DICTIONARIES (unordered key-value pairs, hash tables)

Comparison with PowerShell hash tables:
    Python:      user_config = {"name": "John", "age": 30}
    PowerShell:  $userConfig = @{Name="John"; Age=30}
    
Python uses colons, PowerShell uses equals signs.
Python is slightly shorter and clearer.
"""

user_configuration = {
    "user_name": "JohnDoe",
    "email_address": "john@example.com",
    "department": "Engineering",
    "access_level": "Standard",
    "account_created": "2020-01-15"
}

# DICTIONARY OPERATIONS
user_configuration["last_login"] = "2024-01-20"     # Add/update key
email = user_configuration.get("email_address")     # Safe access (returns None if missing)
email_or_default = user_configuration.get("email", "unknown@example.com")

# DICTIONARY METHODS
all_keys = user_configuration.keys()
all_values = user_configuration.values()
all_items = user_configuration.items()              # Returns key-value pairs

# DICTIONARY COMPREHENSION
squared_dict = {x: x**2 for x in range(5)}          # {0: 0, 1: 1, 2: 4, 3: 9, 4: 16}

"""
SETS (unordered collection of unique elements)

Sets automatically eliminate duplicates and provide fast membership testing.
"""

unique_tags = {"python", "programming", "automation", "scripting"}
unique_tags.add("devops")                           # Add element
unique_tags.remove("automation")                    # Remove (raises error if not found)
unique_tags.discard("automation")                   # Remove (silent if not found)

# SET OPERATIONS
set_a = {1, 2, 3, 4, 5}
set_b = {4, 5, 6, 7, 8}

union_set = set_a | set_b                           # Union: {1, 2, 3, 4, 5, 6, 7, 8}
intersection_set = set_a & set_b                    # Intersection: {4, 5}
difference_set = set_a - set_b                      # Difference: {1, 2, 3}

# ============================================================================
# OPERATORS
# ============================================================================
"""
ARITHMETIC OPERATORS

Same as most languages, but Python has ** for exponentiation.
"""

total_items = 10 + 5                                # Addition: 15
items_remaining = 100 - 25                          # Subtraction: 75
product_price = 19.99 * 3                           # Multiplication: 59.97
average_score = 200 / 4                             # Division (float): 50.0
integer_division = 200 // 4                         # Floor division: 50
remainder_value = 17 % 5                            # Modulus: 2
power_result = 2 ** 8                               # Exponentiation: 256

"""
COMPARISON OPERATORS

Comparison with PowerShell:
    Python:      if age >= 18:
    PowerShell:  if ($age -ge 18)
    
Python uses symbols (==, !=, >, <, >=, <=)
PowerShell uses words (-eq, -ne, -gt, -lt, -ge, -le)
Python is shorter and more like C/Java.
"""

current_age = 25
minimum_age = 18
is_old_enough = current_age >= minimum_age          # True

entered_password = "Secret123"
stored_password = "secret123"
password_matches = entered_password == stored_password          # False (case-sensitive!)
case_insensitive_match = entered_password.lower() == stored_password.lower()  # True

# Python is CASE-SENSITIVE by default (unlike PowerShell)
# PowerShell: "ABC" -eq "abc" → True
# Python:     "ABC" == "abc" → False

"""
LOGICAL OPERATORS

Comparison with PowerShell:
    Python:      if has_license and has_subscription:
    PowerShell:  if ($hasLicense -and $hasSubscription)
    
Both use English words (and, or, not) instead of symbols (&&, ||, !)
Python is cleaner without $ sigils.
"""

has_valid_license = True
has_current_subscription = True
can_access_premium = has_valid_license and has_current_subscription

is_weekend = False
is_holiday = False
is_day_off = is_weekend or is_holiday

service_is_running = True
needs_restart = not service_is_running

"""
MEMBERSHIP AND IDENTITY OPERATORS

in / not in: Test membership in sequences
is / is not: Test object identity (same object in memory)
"""

available_colors = ["Red", "Green", "Blue", "Yellow"]
has_green = "Green" in available_colors             # True
has_purple = "Purple" not in available_colors       # True

# IDENTITY (is vs ==)
# == tests value equality
# is tests object identity (same memory location)
list_a = [1, 2, 3]
list_b = [1, 2, 3]
list_c = list_a

value_equal = list_a == list_b                      # True (same values)
identity_check = list_a is list_b                   # False (different objects)
same_object = list_a is list_c                      # True (same object)

# None should always be checked with 'is'
result = None
if result is None:                                  # Correct
    print("No result")

"""
ASSIGNMENT OPERATORS

Augmented assignment (in-place operation)
"""

total_score = 0
total_score += 10                                   # Add and assign
total_score -= 5                                    # Subtract and assign
total_score *= 2                                    # Multiply and assign
total_score //= 3                                   # Floor divide and assign
total_score %= 7                                    # Modulus and assign

# ============================================================================
# CONTROL FLOW
# ============================================================================
"""
IF-ELIF-ELSE STATEMENT

Python uses indentation to define code blocks (no braces!)
Standard indentation: 4 spaces (PEP 8 requirement)

Comparison with PowerShell:
    Python:      if condition:
                     # 4 space indent
    PowerShell:  if ($condition) {
                     # indent optional
                 }
    
Python is more concise (no $ and {})
Python REQUIRES consistent indentation (syntax error if wrong)
PowerShell indentation is purely stylistic
"""

account_balance = 150.00
minimum_balance = 100.00
transaction_amount = 75.00

if account_balance > (minimum_balance + transaction_amount):
    print("Transaction approved. Sufficient funds available.")
    account_balance -= transaction_amount
elif account_balance > transaction_amount:
    print("Transaction approved with warning. Balance approaching minimum.")
    account_balance -= transaction_amount
else:
    print("Transaction declined. Insufficient funds.")

# TERNARY OPERATOR (conditional expression)
status_message = "Active" if is_account_active else "Inactive"

"""
MATCH STATEMENT (Python 3.10+)

Pattern matching - more powerful than traditional switch statements.
Similar to switch in other languages but with destructuring support.

Note: PowerShell has switch, C# has switch expressions.
"""

from datetime import datetime

day_of_week = datetime.now().strftime("%A")

match day_of_week:
    case "Monday":
        print("Start of work week")
    case "Friday":
        print("Last work day of the week")
    case "Saturday" | "Sunday":                     # Multiple patterns
        print("Weekend day")
    case _:                                         # Default case
        print("Midweek day")

# PATTERN MATCHING WITH DESTRUCTURING
point = (0, 10)

match point:
    case (0, 0):
        print("Origin")
    case (0, y):
        print(f"On Y axis at {y}")
    case (x, 0):
        print(f"On X axis at {x}")
    case (x, y):
        print(f"Point at ({x}, {y})")

"""
FOR LOOP

Python for loops iterate over sequences (not index-based by default).
Most Pythonic way to loop.

Comparison with PowerShell:
    Python:      for server in servers:
    PowerShell:  foreach ($server in $servers) {
    
Python is 10 characters shorter (no $ and {})
"""

server_names = ["WebServer01", "DatabaseServer01", "CacheServer01"]

# ITERATE OVER LIST
for current_server_name in server_names:
    print(f"Processing server: {current_server_name}")

# ITERATE WITH INDEX (using enumerate)
for server_index, current_server_name in enumerate(server_names):
    print(f"Server {server_index + 1} of {len(server_names)}: {current_server_name}")

# ITERATE OVER RANGE (like traditional for loop)
for iteration_number in range(5):                   # 0, 1, 2, 3, 4
    print(f"Iteration: {iteration_number}")

# RANGE WITH START AND STEP
for number in range(10, 20, 2):                     # 10, 12, 14, 16, 18
    print(number)

# ITERATE OVER DICTIONARY
for key, value in user_configuration.items():
    print(f"{key}: {value}")

"""
LIST COMPREHENSIONS (concise loop alternative)

Python's most distinctive feature for creating lists.
More concise than equivalent for loops.

Comparison with PowerShell:
    Python:      evens = [x for x in range(20) if x % 2 == 0]
    PowerShell:  $evens = 0..19 | Where-Object { $_ % 2 -eq 0 }
    
Both are concise, but Python is single expression.
"""

# BASIC LIST COMPREHENSION
squares = [x**2 for x in range(10)]

# WITH CONDITION
even_squares = [x**2 for x in range(10) if x % 2 == 0]

# NESTED COMPREHENSION
matrix = [[row * col for col in range(3)] for row in range(3)]

# DICTIONARY COMPREHENSION
square_dict = {x: x**2 for x in range(5)}

# SET COMPREHENSION
unique_lengths = {len(word) for word in ["hello", "world", "python"]}

"""
WHILE LOOP

Executes while condition is true (same as PowerShell).
"""

attempt_count = 0
max_attempts = 5
connection_successful = False

while not connection_successful and attempt_count < max_attempts:
    attempt_count += 1
    print(f"Connection attempt {attempt_count} of {max_attempts}")
    
    # Simulate connection attempt
    import random
    connection_successful = random.choice([True, False])
    
    if not connection_successful:
        import time
        time.sleep(2)

"""
BREAK AND CONTINUE

Same as PowerShell: break exits loop, continue skips to next iteration.
"""

# Process only even numbers, stop at 15
for current_number in range(1, 21):
    if current_number % 2 != 0:
        continue                                    # Skip odd numbers
    
    if current_number > 15:
        break                                       # Stop after 15
    
    print(f"Processing even number: {current_number}")

"""
ELSE CLAUSE ON LOOPS (unique Python feature!)

Python allows else clause on loops:
- Executes if loop completes normally (no break)
- Does not execute if loop exits via break
"""

search_value = 7
number_list = [1, 3, 5, 7, 9]

for number in number_list:
    if number == search_value:
        print(f"Found {search_value}")
        break
else:
    # This executes only if break was NOT called
    print(f"{search_value} not found in list")

# ============================================================================
# FUNCTIONS
# ============================================================================
"""
FUNCTION DEFINITION

Python uses 'def' keyword to define functions.
No return type declaration (dynamically typed).

Comparison with PowerShell:
    Python:      def get_user_status(user_name):
    PowerShell:  function Get-UserStatus {
                     param([string]$UserName)
    
Python is more concise, no type annotation required.
Python uses snake_case, PowerShell uses Verb-Noun PascalCase.
"""

def get_user_account_status(user_name, include_details=False):
    """
    Retrieve account status information for a user.
    
    Args:
        user_name (str): The username to check. Required parameter.
        include_details (bool): Include detailed information. Defaults to False.
    
    Returns:
        dict: Dictionary with account status information.
    
    Examples:
        >>> get_user_account_status("jdoe")
        {'user_name': 'jdoe', 'account_exists': True, ...}
        
        >>> get_user_account_status("jdoe", include_details=True)
        {'user_name': 'jdoe', 'account_exists': True, 'detailed_info': ...}
    """
    from datetime import datetime, timedelta
    
    print(f"Checking account status for user: {user_name}")
    
    # Create result dictionary
    account_status = {
        "user_name": user_name,
        "account_exists": True,
        "is_active": True,
        "last_login": datetime.now() - timedelta(days=5),
        "checked_at": datetime.now()
    }
    
    if include_details:
        account_status["detailed_info"] = "Extended account information"
    
    return account_status

"""
FUNCTION PARAMETERS

Python supports various parameter types:
- Positional parameters
- Keyword parameters
- Default values
- *args (variable positional arguments)
- **kwargs (variable keyword arguments)
- Positional-only (/) and keyword-only (*) separators
"""

def create_server_connection(
    server_name,                                    # Required positional
    port_number=443,                                # Optional with default
    protocol="HTTPS",                               # Optional with default
    *additional_options,                            # Variable positional args
    timeout=30,                                     # Keyword-only (after *)
    **connection_options                            # Variable keyword args
):
    """
    Create server connection with flexible parameters.
    
    Args:
        server_name: Server hostname (required)
        port_number: Port number (default: 443)
        protocol: Connection protocol (default: HTTPS)
        *additional_options: Additional positional arguments
        timeout: Connection timeout in seconds (keyword-only)
        **connection_options: Additional keyword arguments
    """
    print(f"Connecting to {server_name}:{port_number} using {protocol}")
    print(f"Timeout: {timeout}s")
    print(f"Additional options: {additional_options}")
    print(f"Connection options: {connection_options}")

# Function calls with different parameter combinations
create_server_connection("server01")
create_server_connection("server02", 8080, "HTTP", timeout=60)
create_server_connection("server03", retry_count=3, use_ssl=True)

"""
TYPE HINTS (Python 3.5+)

Type hints provide static type checking (with mypy) but don't enforce at runtime.
Makes code more self-documenting.
"""

from typing import List, Dict, Optional, Union, Tuple

def process_user_data(
    user_names: List[str],
    user_ages: Dict[str, int],
    default_age: Optional[int] = None
) -> List[Tuple[str, int]]:
    """
    Process user data with type hints.
    
    Args:
        user_names: List of user names
        user_ages: Dictionary mapping names to ages
        default_age: Default age if not found (optional)
    
    Returns:
        List of tuples (name, age)
    """
    result_list = []
    
    for user_name in user_names:
        user_age = user_ages.get(user_name, default_age)
        if user_age is not None:
            result_list.append((user_name, user_age))
    
    return result_list

"""
LAMBDA FUNCTIONS (anonymous functions)

Single-expression functions for simple operations.

Comparison with PowerShell:
    Python:      square = lambda x: x**2
    PowerShell:  $square = { param($x) $x * $x }
    
Python lambda is more concise for simple cases.
"""

# LAMBDA FUNCTION
square_number = lambda x: x**2
add_numbers = lambda a, b: a + b

result = square_number(5)                           # 25
sum_result = add_numbers(10, 20)                    # 30

# LAMBDA IN SORTED
users = [
    {"name": "Alice", "age": 30},
    {"name": "Bob", "age": 25},
    {"name": "Charlie", "age": 35}
]

# Sort by age using lambda
sorted_users = sorted(users, key=lambda user: user["age"])

# LAMBDA IN FILTER AND MAP
numbers = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
even_numbers = list(filter(lambda x: x % 2 == 0, numbers))
squared_numbers = list(map(lambda x: x**2, numbers))

"""
DECORATORS (function wrappers)

Python's elegant way to modify function behavior.
Functions are first-class objects.
"""

import functools
import time

def timing_decorator(function_to_time):
    """Decorator that measures function execution time."""
    @functools.wraps(function_to_time)
    def wrapper_function(*args, **kwargs):
        start_time = time.perf_counter()
        result = function_to_time(*args, **kwargs)
        end_time = time.perf_counter()
        elapsed_time = end_time - start_time
        print(f"{function_to_time.__name__} executed in {elapsed_time:.4f}s")
        return result
    return wrapper_function

@timing_decorator
def slow_calculation(number_of_iterations):
    """Simulate slow calculation."""
    total = 0
    for i in range(number_of_iterations):
        total += i**2
    return total

# ============================================================================
# ERROR HANDLING
# ============================================================================
"""
TRY-EXCEPT-ELSE-FINALLY

Python's exception handling mechanism.

Comparison with PowerShell:
    Python:      try / except / else / finally
    PowerShell:  try / catch / finally
    
Python has 'else' clause (executes if no exception).
PowerShell doesn't have else clause.
"""

def read_configuration_file(config_file_path):
    """
    Read configuration file with error handling.
    
    Args:
        config_file_path: Path to configuration file
    
    Returns:
        str: Configuration content or None if error
    """
    try:
        print(f"Attempting to read configuration file: {config_file_path}")
        
        with open(config_file_path, 'r') as config_file:
            configuration_content = config_file.read()
        
        if not configuration_content.strip():
            raise ValueError("Configuration file is empty")
        
        print("Configuration loaded successfully")
        return configuration_content
    
    except FileNotFoundError:
        print(f"Configuration file not found: {config_file_path}")
        return None
    
    except PermissionError:
        print(f"Access denied to configuration file: {config_file_path}")
        return None
    
    except ValueError as value_error:
        print(f"Invalid configuration: {value_error}")
        return None
    
    except Exception as general_exception:
        # Catch-all for unexpected exceptions
        print(f"Unexpected error: {general_exception}")
        print(f"Error type: {type(general_exception).__name__}")
        return None
    
    else:
        # Executes only if no exception was raised
        print("No errors encountered during file reading")
    
    finally:
        # Always executes, whether exception occurred or not
        print("Completed configuration file read attempt")

"""
RAISE STATEMENT

Explicitly raise exceptions to stop execution.

Comparison with PowerShell:
    Python:      raise ValueError("message")
    PowerShell:  throw "message"
    
Python uses specific exception types.
PowerShell throw is more generic.
"""

def set_database_connection(connection_string):
    """Set database connection with validation."""
    if not connection_string:
        raise ValueError("ConnectionString cannot be null or empty")
    
    if "password=" not in connection_string.lower():
        raise ValueError("Connection string must contain password")
    
    print("Connection established")

"""
CUSTOM EXCEPTIONS

Create domain-specific exception classes.
"""

class InsufficientFundsError(Exception):
    """Raised when account has insufficient funds for transaction."""
    def __init__(self, balance, amount):
        self.balance = balance
        self.amount = amount
        self.shortage = amount - balance
        super().__init__(f"Insufficient funds: need ${amount}, have ${balance}")

class AccountLockedError(Exception):
    """Raised when account is locked."""
    pass

def withdraw_funds(account_balance, withdrawal_amount):
    """Withdraw funds with custom exception handling."""
    if withdrawal_amount > account_balance:
        raise InsufficientFundsError(account_balance, withdrawal_amount)
    
    return account_balance - withdrawal_amount

# ============================================================================
# OBJECT-ORIENTED PROGRAMMING
# ============================================================================
"""
CLASS DEFINITION

Python supports full object-oriented programming.

Comparison with PowerShell:
    Both support classes (PowerShell 5.0+)
    Python: class ClassName:
    PowerShell: class ClassName {
    
Python uses def for methods, __init__ for constructor.
PowerShell uses constructor name matching class name.
"""

from enum import Enum
from datetime import datetime

class AccountType(Enum):
    """Account type enumeration."""
    STANDARD = 1
    PREMIUM = 2
    ENTERPRISE = 3

class TransactionStatus(Enum):
    """Transaction status enumeration."""
    PENDING = 1
    APPROVED = 2
    DECLINED = 3
    CANCELLED = 4

class BankAccount:
    """
    Bank account class with balance management.
    
    Attributes:
        account_number (str): Unique account identifier
        account_holder_name (str): Name of account holder
        account_type (AccountType): Type of account
        balance (float): Current account balance
        created_date (datetime): Account creation date
        is_active (bool): Account active status
    """
    
    # CLASS VARIABLE (shared by all instances)
    total_accounts_created = 0
    minimum_balance = 0.0
    
    def __init__(self, account_number, account_holder_name, account_type):
        """
        Initialize bank account.
        
        Args:
            account_number: Unique account identifier
            account_holder_name: Name of account holder
            account_type: Type of account
        """
        # INSTANCE VARIABLES (unique to each instance)
        self.account_number = account_number
        self.account_holder_name = account_holder_name
        self.account_type = account_type
        self.balance = 0.0
        self.created_date = datetime.now()
        self.is_active = True
        
        # Increment class variable
        BankAccount.total_accounts_created += 1
    
    def deposit(self, deposit_amount):
        """
        Deposit funds into account.
        
        Args:
            deposit_amount: Amount to deposit
        
        Raises:
            ValueError: If deposit amount is not positive
            AccountLockedError: If account is not active
        """
        if deposit_amount <= 0:
            raise ValueError("Deposit amount must be positive")
        
        if not self.is_active:
            raise AccountLockedError("Cannot deposit to inactive account")
        
        self.balance += deposit_amount
        print(f"Deposited ${deposit_amount}. New balance: ${self.balance}")
    
    def withdraw(self, withdrawal_amount):
        """
        Withdraw funds from account.
        
        Args:
            withdrawal_amount: Amount to withdraw
        
        Returns:
            TransactionStatus: Status of withdrawal transaction
        """
        if withdrawal_amount <= 0:
            raise ValueError("Withdrawal amount must be positive")
        
        if not self.is_active:
            return TransactionStatus.DECLINED
        
        if self.balance >= withdrawal_amount:
            self.balance -= withdrawal_amount
            print(f"Withdrew ${withdrawal_amount}. New balance: ${self.balance}")
            return TransactionStatus.APPROVED
        else:
            print("Insufficient funds for withdrawal")
            return TransactionStatus.DECLINED
    
    def get_account_summary(self):
        """
        Get account summary string.
        
        Returns:
            str: Formatted account summary
        """
        return (f"Account: {self.account_number} | "
                f"Holder: {self.account_holder_name} | "
                f"Balance: ${self.balance} | "
                f"Type: {self.account_type.name}")
    
    @classmethod
    def get_total_accounts(cls):
        """
        Get total number of accounts created.
        
        Returns:
            int: Total accounts created
        """
        return cls.total_accounts_created
    
    @staticmethod
    def validate_account_number(account_number):
        """
        Validate account number format.
        
        Args:
            account_number: Account number to validate
        
        Returns:
            bool: True if valid, False otherwise
        """
        return len(account_number) >= 5 and account_number.isalnum()
    
    def __str__(self):
        """String representation for print()."""
        return self.get_account_summary()
    
    def __repr__(self):
        """Developer-friendly representation."""
        return f"BankAccount('{self.account_number}', '{self.account_holder_name}')"

"""
INHERITANCE

Python supports single and multiple inheritance.
"""

class SavingsAccount(BankAccount):
    """Savings account with interest."""
    
    def __init__(self, account_number, account_holder_name, interest_rate):
        """Initialize savings account with interest rate."""
        # Call parent constructor
        super().__init__(account_number, account_holder_name, AccountType.STANDARD)
        self.interest_rate = interest_rate
        self.minimum_balance = 100.0
    
    def apply_interest(self):
        """Apply interest to account balance."""
        interest_amount = self.balance * (self.interest_rate / 100)
        self.balance += interest_amount
        print(f"Applied interest: ${interest_amount}. New balance: ${self.balance}")
    
    def withdraw(self, withdrawal_amount):
        """Override withdraw to check minimum balance."""
        if self.balance - withdrawal_amount < self.minimum_balance:
            print(f"Cannot withdraw: minimum balance ${self.minimum_balance} required")
            return TransactionStatus.DECLINED
        
        return super().withdraw(withdrawal_amount)

# USING CLASSES
checking_account = BankAccount("CHK-12345", "John Doe", AccountType.PREMIUM)
checking_account.deposit(1000.00)
withdrawal_status = checking_account.withdraw(250.00)

savings_account = SavingsAccount("SAV-67890", "Jane Smith", 2.5)
savings_account.deposit(5000.00)
savings_account.apply_interest()

print(f"Total accounts created: {BankAccount.get_total_accounts()}")

# ============================================================================
# MODULES AND PACKAGES
# ============================================================================
"""
MODULES

Python code is organized into modules (.py files).
A module is a single Python file.
A package is a directory containing __init__.py and modules.

Comparison with PowerShell:
    Python:      import module_name
    PowerShell:  Import-Module ModuleName
    
Both have similar concepts, but Python's import is more granular.
"""

# IMPORT ENTIRE MODULE
import math
result = math.sqrt(16)

# IMPORT SPECIFIC FUNCTION
from math import sqrt, pi
result = sqrt(16)

# IMPORT WITH ALIAS
import datetime as dt
current_time = dt.datetime.now()

# IMPORT ALL (not recommended - can cause namespace pollution)
from math import *

"""
__name__ == "__main__" IDIOM

Allows module to be imported or run as script.
"""

def main_function():
    """Main entry point for script."""
    print("Running as main program")

if __name__ == "__main__":
    # This block only runs when script is executed directly
    # Not when imported as module
    main_function()

# ============================================================================
# CONTEXT MANAGERS (with statement)
# ============================================================================
"""
WITH STATEMENT

Ensures proper resource cleanup (like try-finally).
Commonly used for file handling, locks, connections.

Comparison with PowerShell:
    Python has dedicated 'with' syntax
    PowerShell typically uses try-finally manually
    
Python's with statement is more elegant.
"""

# FILE HANDLING WITH CONTEXT MANAGER
with open("config.txt", "r") as config_file:
    configuration_data = config_file.read()
    # File automatically closed when exiting 'with' block

# MULTIPLE CONTEXT MANAGERS
with open("input.txt", "r") as input_file, \
     open("output.txt", "w") as output_file:
    content = input_file.read()
    output_file.write(content.upper())

"""
CUSTOM CONTEXT MANAGER

Create custom context managers with __enter__ and __exit__ methods.
"""

class DatabaseConnection:
    """Database connection context manager."""
    
    def __init__(self, connection_string):
        self.connection_string = connection_string
        self.connection = None
    
    def __enter__(self):
        """Called when entering 'with' block."""
        print(f"Opening database connection: {self.connection_string}")
        self.connection = f"Connection to {self.connection_string}"
        return self.connection
    
    def __exit__(self, exc_type, exc_value, traceback):
        """Called when exiting 'with' block."""
        print("Closing database connection")
        self.connection = None
        return False  # Don't suppress exceptions

# Usage
with DatabaseConnection("localhost:5432/mydb") as db_connection:
    print(f"Using {db_connection}")

# ============================================================================
# ADVANCED FEATURES
# ============================================================================

"""
GENERATORS (lazy evaluation)

Generators produce values on-demand (memory efficient).
Use 'yield' instead of 'return'.
"""

def generate_fibonacci_sequence(count):
    """Generate Fibonacci sequence up to count numbers."""
    first_number, second_number = 0, 1
    for _ in range(count):
        yield first_number
        first_number, second_number = second_number, first_number + second_number

# Generators are iterators (can use in for loop)
for fib_number in generate_fibonacci_sequence(10):
    print(fib_number, end=" ")

# GENERATOR EXPRESSION (like list comprehension but lazy)
squared_generator = (x**2 for x in range(1000000))  # Memory efficient
first_ten_squares = list(x for i, x in enumerate(squared_generator) if i < 10)

"""
ITERATORS

Objects that implement __iter__() and __next__() methods.
"""

class CountdownIterator:
    """Iterator that counts down from start value."""
    
    def __init__(self, start_value):
        self.current_value = start_value
    
    def __iter__(self):
        """Return iterator object (self)."""
        return self
    
    def __next__(self):
        """Return next value or raise StopIteration."""
        if self.current_value <= 0:
            raise StopIteration
        
        value = self.current_value
        self.current_value -= 1
        return value

# Usage
for count in CountdownIterator(5):
    print(count, end=" ")  # 5 4 3 2 1

"""
LIST SLICING (powerful feature)

Python's slicing is very flexible and concise.
"""

numbers = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9]

first_five = numbers[:5]                            # [0, 1, 2, 3, 4]
last_five = numbers[-5:]                            # [5, 6, 7, 8, 9]
middle = numbers[3:7]                               # [3, 4, 5, 6]
every_second = numbers[::2]                         # [0, 2, 4, 6, 8]
reversed_list = numbers[::-1]                       # [9, 8, 7, 6, 5, 4, 3, 2, 1, 0]

"""
UNPACKING OPERATORS (* and **)

Powerful operators for function arguments and container unpacking.
"""

# UNPACKING LISTS
def sum_three_numbers(a, b, c):
    return a + b + c

number_list = [1, 2, 3]
result = sum_three_numbers(*number_list)            # Unpacks list as arguments

# UNPACKING DICTIONARIES
def create_user(name, age, email):
    return {"name": name, "age": age, "email": email}

user_data = {"name": "Alice", "age": 30, "email": "alice@example.com"}
new_user = create_user(**user_data)                 # Unpacks dict as keyword args

# MERGE LISTS
list1 = [1, 2, 3]
list2 = [4, 5, 6]
merged = [*list1, *list2]                           # [1, 2, 3, 4, 5, 6]

# MERGE DICTIONARIES (Python 3.5+)
dict1 = {"a": 1, "b": 2}
dict2 = {"c": 3, "d": 4}
merged_dict = {**dict1, **dict2}                    # {"a": 1, "b": 2, "c": 3, "d": 4}

# ============================================================================
# COMPARISON WITH POWERSHELL (CHARACTER COUNT)
# ============================================================================
"""
CHARACTER COUNT COMPARISON FOR COMMON OPERATIONS

1. VARIABLE ASSIGNMENT
   Python:      user_name = "John"                (21 chars)
   PowerShell:  $userName = "John"                (22 chars)
   Winner: Python (1 char shorter, no $)

2. ARRAY/LIST CREATION
   Python:      items = [1, 2, 3]                 (20 chars)
   PowerShell:  $items = @(1, 2, 3)               (23 chars)
   Winner: Python (3 chars shorter, no $ and @)

3. DICTIONARY/HASH TABLE
   Python:      d = {"name": "John"}              (22 chars)
   PowerShell:  $d = @{Name="John"}               (21 chars)
   Winner: PowerShell (1 char shorter)

4. FUNCTION DEFINITION
   Python:      def get_data():                   (16 chars)
   PowerShell:  function Get-Data {               (20 chars)
   Winner: Python (4 chars shorter)

5. LOOP THROUGH ARRAY
   Python:      for item in array:                (21 chars)
   PowerShell:  foreach ($item in $array) {       (29 chars)
   Winner: Python (8 chars shorter, significant!)

6. CONDITIONAL STATEMENT
   Python:      if x == 5:                        (10 chars)
   PowerShell:  if ($x -eq 5) {                   (14 chars)
   Winner: Python (4 chars shorter)

7. LIST COMPREHENSION vs Pipeline
   Python:      [x*2 for x in nums]               (20 chars)
   PowerShell:  $nums | % { $_ * 2 }              (21 chars)
   Winner: Python (1 char shorter)

OVERALL WINNER: Python (shorter for most operations)

Python's advantages:
- No variable sigils ($) saves characters
- Symbolic operators (==, !=) vs word operators (-eq, -ne)
- Indentation-based syntax (no braces)
- More concise for comprehensions

PowerShell's advantages:
- More discoverable (Verb-Noun naming)
- Better for system administration
- Object pipeline more powerful than text
"""

# ============================================================================
# STYLE GUIDE (PEP 8)
# ============================================================================
"""
PYTHON STYLE GUIDE BEST PRACTICES (PEP 8)

NAMING CONVENTIONS:
- Functions: snake_case (get_user_data, calculate_total)
- Variables: snake_case (user_name, total_count)
- Constants: ALL_CAPS_WITH_UNDERSCORES (MAX_CONNECTIONS, API_KEY)
- Classes: PascalCase (BankAccount, UserProfile)
- Private: _leading_underscore (_private_method, _internal_variable)
- Methods: snake_case (same as functions)

FORMATTING:
- Indentation: 4 spaces (REQUIRED, not optional!)
- Line length: 79 characters for code, 72 for comments
- Blank lines: 2 before top-level functions/classes, 1 between methods
- Spaces: Around operators (x = 5, not x=5)
- No spaces: Inside parentheses/brackets [(1, 2), not [( 1, 2 )]

IMPORTS:
- Standard library first
- Third-party libraries second
- Local application imports third
- Separate groups with blank line
- One import per line (except from imports)

COMMENTS:
- Docstrings for all public modules, functions, classes, methods
- Use triple quotes (""") for docstrings
- Single-line comments: # before the line
- Inline comments: two spaces before #

STRINGS:
- Use double quotes for user-facing strings
- Use single quotes for internal strings (convention varies)
- Triple quotes for multi-line strings
- Use f-strings for formatting (Python 3.6+)

WHITESPACE:
- No trailing whitespace
- Blank line at end of file
- No multiple blank lines in code

BEST PRACTICES:
- Use 'is' for None comparisons (if x is None)
- Use 'not in' for membership (if key not in dict)
- Use context managers (with) for resource management
- Prefer list comprehensions over map/filter (when simple)
- Use enumerate() instead of range(len())
- Use items() for dict iteration
- Keep functions small and focused (single responsibility)

PYTHONIC IDIOMS:
- Use enumerate() for index+value loops
- Use zip() to iterate multiple sequences
- Use unpacking for swaps: a, b = b, a
- Use get() with default for dicts
- Use 'in' for membership tests
- Use list comprehensions for transformations
- Use generators for large datasets
"""

# ============================================================================
# DESIGN PRINCIPLES
# ============================================================================
"""
PYTHON DESIGN PRINCIPLES (Zen of Python - PEP 20)

Execute this in Python REPL: import this

1. Beautiful is better than ugly
   - Write aesthetically pleasing code
   
2. Explicit is better than implicit
   - Be clear about what code does
   
3. Simple is better than complex
   - Favor simple solutions
   
4. Complex is better than complicated
   - If complexity is needed, keep it organized
   
5. Flat is better than nested
   - Avoid deep nesting
   
6. Sparse is better than dense
   - Don't pack too much in one line
   
7. Readability counts
   - Code is read more than written
   
8. Special cases aren't special enough to break the rules
   - Be consistent
   
9. Although practicality beats purity
   - Pragmatism over dogmatism
   
10. Errors should never pass silently
    - Always handle errors explicitly
    
11. Unless explicitly silenced
    - But allow intentional error suppression
    
12. In the face of ambiguity, refuse the temptation to guess
    - Be explicit rather than assuming
    
13. There should be one-- and preferably only one --obvious way to do it
    - Consistency in approach
    
14. Although that way may not be obvious at first unless you're Dutch
    - Reference to Guido van Rossum (Python's creator)
    
15. Now is better than never
    - Don't over-plan, start coding
    
16. Although never is often better than *right* now
    - But don't rush without thinking
    
17. If the implementation is hard to explain, it's a bad idea
    - Complexity indicates poor design
    
18. If the implementation is easy to explain, it may be a good idea
    - Simplicity indicates good design
    
19. Namespaces are one honking great idea
    - Use modules and packages to organize code
"""

# ============================================================================
# FINAL NOTES
# ============================================================================
"""
This comprehensive reference covers Python syntax from basics to advanced topics.
All examples use semantically meaningful variable names for clarity.

Python excels at:
- Rapid development and prototyping
- Data science and machine learning
- Web development (Django, Flask)
- Automation and scripting
- Scientific computing
- Readable, maintainable code

Key takeaways:
1. Python uses indentation as syntax (REQUIRED)
2. Dynamic typing with optional type hints
3. Snake_case naming convention
4. "Batteries included" standard library
5. List comprehensions for concise transformations
6. First-class functions and decorators
7. Generator expressions for memory efficiency

Comparison with PowerShell:
- Python: General-purpose, multi-paradigm
- PowerShell: System administration focused
- Python: Shorter syntax (no $ sigils)
- PowerShell: Object pipeline (richer than text)
- Python: snake_case naming
- PowerShell: Verb-Noun PascalCase naming
- Python: Required indentation
- PowerShell: Optional indentation

For expanding this code:
- Variable names clearly indicate purpose
- Functions follow single responsibility principle
- Comments explain "why" not "what"
- Modular structure supports easy enhancement
- Type hints document expected types
- Docstrings provide API documentation
"""

# Execute main function if run as script
if __name__ == "__main__":
    print("Python Syntax Reference - Complete Implementation")
    print("=" * 70)
    print("This file demonstrates all Python syntax with detailed explanations")
    print("Compare with PowerShell syntax-reference.ps1 to see differences")
    print("=" * 70)
