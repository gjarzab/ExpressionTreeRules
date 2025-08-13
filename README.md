# Expression Tree Rules

A lightweight and extensible rule engine for .NET. This engine dynamically compiles rules defined as JSON into executable .NET code at runtime.

## Table of Contents

- [Core Features](#core-features)
- [Getting Started](#getting-started)
- [Usage Guidance: Separate Evaluation from Side Effects](#usage-guidance-separate-evaluation-from-side-effects)
- [How It Works](#how-it-works)
- [Error Handling](#error-handling)
- [Potential Use Cases](#potential-use-cases)
- [Contributing](#contributing)
- [License](#license)

---

## Core Features

- **JSON-Based Rule Definition** – Define rules in a human-readable JSON format.
- **High-Performance Compilation** – Rules are compiled into LINQ Expression Trees and then into highly optimized delegates using `FastExpressionCompiler`. No interpretation at runtime.
- **Extensible and Type-Safe** – Execute rules against strongly typed C# context objects for full type safety and direct access to your application’s domain model.
- **Dynamic Metadata Generation** – Automatically generate descriptions of available types, members, and methods from a context object, useful for building dynamic UIs such as rule editors.
- **Rich Expression Support**, including:
  - Binary operators: `+`, `-`, `==`, `!=`, `>`, `>=`, `<`, `<=`, `&&`, `||`
  - Unary operator: `!`
  - Method calls on the context object, including nested members and extension methods
  - Member and field access
  - Literals: string, integer, decimal, boolean, and arrays

---

## Getting Started

Here’s a minimal example of defining, compiling, and executing a rule.

### 1. Define Your Context Class

```csharp
using Core;
using Metadata;

public class ServiceContext : IContext
{
    public string ContextName => "ServiceContext";
    public int ErrorCount { get; set; }
    public bool IsEnabled { get; set; } = true;

    [MethodDescription("Disables the service.")]
    public void DisableService() => IsEnabled = false;
}
```

### 2. Define a Rule in JSON

```json
{
  "name": "Disable service if error count is too high",
  "condition": {
    "expressionType": "Binary",
    "operator": "GreaterThan",
    "left": { "expressionType": "MemberAccess", "path": "ErrorCount" },
    "right": { "expressionType": "BasicLiteral", "kind": "INT", "value": "5" }
  },
  "actions": [
    { "expressionType": "Call", "method": { "name": "DisableService" }, "arguments": [] }
  ]
}
```

### 3. Compile and Execute

```csharp
using Newtonsoft.Json.Linq;
using Compiler.Options;
using Compiler.Services;

var context = new ServiceContext { ErrorCount = 10 };
var jsonRule = JObject.Parse("..."); // Load JSON above

var compiler = new RuleCompiler(
    MethodResolutionOptions.Default(typeof(ServiceContext)),
    new LiteralExpressionCompiler()
);
var compiledRule = compiler.CompileRule<ServiceContext>(jsonRule);

compiledRule.EvaluateAndExecute(context);
// context.IsEnabled is now false
```

---

## Usage Guidance: Separate Evaluation from Side Effects

**Rules and actions should not perform I/O**  
Instead, rules should **produce instructions** for the application to process separately.

### Benefits of This Approach

- **Testability** – Rules can be tested in isolation without external dependencies.
- **Performance** – Execution remains fast because no I/O occurs while evaluating conditions or actions.
- **Flexibility** – The same evaluated instructions can be processed in different ways depending on the execution environment.
- **Safety** – Prevents rules from making unapproved or unsafe system changes directly.

### Pattern

1. Rules evaluate conditions and add *instructions* (pure data objects) to the context.
2. The application retrieves these instructions after evaluation and performs the actual work.

---

### Example: Scheduling Preventive Maintenance

#### Domain Classes

```csharp
public class Customer
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class Equipment
{
    public string EquipmentType { get; set; } // e.g., "Furnace", "AC Unit"
    public DateTime InstallDate { get; set; }
    public DateTime LastServiceDate { get; set; }
}
```

#### Action Instruction Classes

```csharp
public interface IActionInstruction { }

public class CreateWorkOrderInstruction : IActionInstruction
{
    public Customer Customer { get; }
    public Equipment Equipment { get; }
    public string WorkOrderType { get; }

    public CreateWorkOrderInstruction(Customer customer, Equipment equipment, string workOrderType)
    {
        Customer = customer;
        Equipment = equipment;
        WorkOrderType = workOrderType;
    }

    public override string ToString() =>
        $"Create Work Order: {WorkOrderType} for {Customer.Name} - {Equipment.EquipmentType}";
}

public class SendCustomerNotificationInstruction : IActionInstruction
{
    public Customer Customer { get; }
    public string MessageTemplate { get; }

    public SendCustomerNotificationInstruction(Customer customer, string messageTemplate)
    {
        Customer = customer;
        MessageTemplate = messageTemplate;
    }

    public override string ToString() =>
        $"Send Notification: Template '{MessageTemplate}' to {Customer.Name}";
}
```

#### Context Implementation

```csharp
using Core;
using Metadata;

public class MaintenanceContext : IContext
{
    private readonly List<IActionInstruction> _instructions = new();

    public string ContextName => "MaintenanceContext";
    public Customer Customer { get; set; }
    public Equipment Equipment { get; set; }
    public DateTime CurrentDate { get; set; }

    public IReadOnlyList<IActionInstruction> Instructions => _instructions;

    [MethodDescription("Creates a work order instruction.")]
    public void CreateWorkOrder(string workOrderType) =>
        _instructions.Add(new CreateWorkOrderInstruction(Customer, Equipment, workOrderType));

    [MethodDescription("Adds a customer notification instruction.")]
    public void SendCustomerNotification(string messageTemplate) =>
        _instructions.Add(new SendCustomerNotificationInstruction(Customer, messageTemplate));
}
```

#### Extension Methods

```csharp
public static class MaintenanceContextExtensions
{
    [MethodDescription("Checks if the equipment is of the specified type.")]
    public static bool IsEquipmentType(this MaintenanceContext ctx, string type) =>
        ctx.Equipment.EquipmentType == type;

    [MethodDescription("Checks if service is overdue by N months.")]
    public static bool IsServiceOverdue(this MaintenanceContext ctx, int months) =>
        ctx.Equipment.LastServiceDate < ctx.CurrentDate.AddMonths(-months);

    [MethodDescription("Checks if it's the pre-winter season (Sept/Oct).")]
    public static bool IsInPreWinterSeason(this MaintenanceContext ctx) =>
        ctx.CurrentDate.Month is >= 9 and <= 10;
}
```

#### JSON Rule

```json
{
  "name": "Schedule Annual Furnace Tune-Up",
  "condition": {
    "expressionType": "Binary",
    "operator": "AndAlso",
    "left": {
      "expressionType": "Binary",
      "operator": "AndAlso",
      "left": {
        "expressionType": "Call",
        "method": { "name": "IsEquipmentType" },
        "arguments": [{ "expressionType": "BasicLiteral", "kind": "STRING", "value": "Furnace" }]
      },
      "right": {
        "expressionType": "Call",
        "method": { "name": "IsServiceOverdue" },
        "arguments": [{ "expressionType": "BasicLiteral", "kind": "INT", "value": "11" }]
      }
    },
    "right": {
      "expressionType": "Call",
      "method": { "name": "IsInPreWinterSeason" },
      "arguments": []
    }
  },
  "actions": [
    {
      "expressionType": "Call",
      "method": { "name": "CreateWorkOrder" },
      "arguments": [{ "expressionType": "BasicLiteral", "kind": "STRING", "value": "Furnace Tune-Up" }]
    },
    {
      "expressionType": "Call",
      "method": { "name": "SendCustomerNotification" },
      "arguments": [{ "expressionType": "BasicLiteral", "kind": "STRING", "value": "Furnace Check-up Reminder" }]
    }
  ]
}
```

#### Safe Execution Pattern

```csharp
using Newtonsoft.Json.Linq;
using Compiler.Options;
using Compiler.Services;

var jsonRule = JObject.Parse("..."); // JSON above

var compiler = new RuleCompiler(
    MethodResolutionOptions.WithExtensionMethods(typeof(MaintenanceContext), typeof(MaintenanceContextExtensions)),
    new LiteralExpressionCompiler()
);

// Compiled rules can be cached
var compiledRule = compiler.CompileRule<MaintenanceContext>(jsonRule);

var customer = new Customer { Name = "John Smith", Email = "john@example.com" };
var equipment = new Equipment {
    EquipmentType = "Furnace",
    InstallDate = new DateTime(2018, 1, 1),
    LastServiceDate = new DateTime(2024, 7, 15)
};

var context = new MaintenanceContext {
    Customer = customer,
    Equipment = equipment,
    CurrentDate = new DateTime(2025, 9, 1)
};

compiledRule.EvaluateAndExecute(context);

// Application processes instructions separately
foreach (var instruction in context.Instructions)
{
    Console.WriteLine(instruction); // Logging, or route to appropriate handler
}
```

---

#### Example Context Description (JSON)

Here is an example of the JSON representation of the `ContextDescription` object for a `UserContext`:

```json
{
  "name": "ServiceContext",
  "members": [
    {
      "name": "ErrorCount",
      "type": "INT",
      "path": "ErrorCount",
      "members": [],
      "methods": []
    },
    {
      "name": "IsEnabled",
      "type": "BOOL",
      "path": "IsEnabled",
      "members": [],
      "methods": []
    }
  ],
  "methods": [
    {
      "name": "DisableService",
      "description": "Disables the service.",
      "returnType": "VOID",
      "parameters": []
    }
  ]
}
```

---

## How It Works

1. **Parse** JSON rule into an AST representation.
2. **Build Expressions** for both conditions and actions.
3. **Compile** expressions into delegates (`Func<IContext,bool>` and `Action<IContext>`).
4. **Execute** compiled rules against context.

---

## Error Handling

During the compilation process you may encounter the following exceptions:

- `ExpressionException` – Unsupported operators or type mismatches.
- `LiteralParseException` – Invalid literal values.
- `MethodResolutionException` – Method not found.

Wrap rule compilation and evaluation in `try-catch` to handle gracefully.

---

## Potential Use Cases

- Business rule management without redeployment
- Dynamic workflows or UI logic
- Complex filtering/validation
- Attribute-based access control (ABAC)

---

## Contributing

Contributions are welcome!

## License

This project is licensed under the MIT License - see the LICENSE file for details.