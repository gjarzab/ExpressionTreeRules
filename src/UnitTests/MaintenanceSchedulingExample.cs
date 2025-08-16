using Compiler.Options;
using Compiler.Services;
using Core;
using Metadata;
using Newtonsoft.Json.Linq;

namespace UnitTests
{
    // --- Domain Objects ---
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

    // --- Action Instructions (The Output) ---
    public interface IActionInstruction { }

    public class CreateWorkOrderInstruction(Customer customer, Equipment equipment, string workOrderType)
        : IActionInstruction
    {
        public Customer Customer { get; } = customer;
        public Equipment Equipment { get; } = equipment;
        public string WorkOrderType { get; } = workOrderType;

        public override string ToString() =>
            $"Create Work Order: {WorkOrderType} for {Customer.Name} - {Equipment.EquipmentType}";
    }

    public class SendCustomerNotificationInstruction(Customer customer, string messageTemplate) : IActionInstruction
    {
        public Customer Customer { get; } = customer;
        public string MessageTemplate { get; } = messageTemplate;

        public override string ToString() => $"Send Notification: Template '{MessageTemplate}' to {Customer.Name}";
    }


    // --- The Context ---
    public class MaintenanceContext : IContext
    {
        private readonly List<IActionInstruction> _instructions = new();

        public string ContextName => "MaintenanceContext";
        public Customer Customer { get; set; }
        public Equipment Equipment { get; set; }
        public DateTime CurrentDate { get; set; }

        public IReadOnlyList<IActionInstruction> Instructions => _instructions;

        [MethodDescription("Creates a new work order for the customer and equipment.")]
        public void CreateWorkOrder(string workOrderType)
        {
            _instructions.Add(new CreateWorkOrderInstruction(Customer, Equipment, workOrderType));
        }

        [MethodDescription("Sends a notification to the customer.")]
        public void SendCustomerNotification(string messageTemplate)
        {
            _instructions.Add(new SendCustomerNotificationInstruction(Customer, messageTemplate));
        }
    }

    // --- Extension Methods ---
    public static class MaintenanceContextExtensions
    {
        [MethodDescription("Checks the type of equipment.")]
        public static bool IsEquipmentType(this MaintenanceContext ctx, string type) => ctx.Equipment.EquipmentType == type;

        [MethodDescription("Checks if the last service was more than a specified number of months ago.")]
        public static bool IsServiceOverdue(this MaintenanceContext ctx, int months) =>
            ctx.Equipment.LastServiceDate < ctx.CurrentDate.AddMonths(-months);

        [MethodDescription("Checks if the current date is in the pre-winter season (Sept/Oct).")]
        public static bool IsInPreWinterSeason(this MaintenanceContext ctx) =>
            ctx.CurrentDate.Month >= 9 && ctx.CurrentDate.Month <= 10;
    }
    
    public class MaintenanceSchedulingExample
    {
        [Fact]
        public void Should_Generate_WorkOrder_And_Notification_For_Overdue_Furnace_In_Season()
        {
            // Arrange
            // If: IsEquipmentType("Furnace") and IsServiceOverdue(11) and IsInPreWinterSeason()
            // Then:
            //      - CreateWorkOrder("Furnace Tune-Up")
            //      - SendCustomerNotification("Furnace Check-up Reminder")
            var ruleJson = new JObject
            {
                ["name"] = "Schedule Annual Furnace Tune-Up",
                ["condition"] = new JObject
                {
                    ["expressionType"] = "Binary",
                    ["operator"] = "AndAlso",
                    ["left"] = new JObject
                    {
                        ["expressionType"] = "Binary",
                        ["operator"] = "AndAlso",
                        ["left"] = new JObject
                        {
                            ["expressionType"] = "Call",
                            ["method"] = new JObject { ["name"] = "IsEquipmentType" },
                            ["arguments"] = new JArray
                            {
                                new JObject
                                {
                                    ["expressionType"] = "BasicLiteral",
                                    ["kind"] = "STRING",
                                    ["value"] = "Furnace"
                                }
                            }
                        },
                        ["right"] = new JObject
                        {
                            ["expressionType"] = "Call",
                            ["method"] = new JObject { ["name"] = "IsServiceOverdue" },
                            ["arguments"] = new JArray
                            {
                                new JObject
                                {
                                    ["expressionType"] = "BasicLiteral",
                                    ["kind"] = "INT",
                                    ["value"] = "11"
                                }
                            }
                        }
                    },
                    ["right"] = new JObject
                    {
                        ["expressionType"] = "Call",
                        ["method"] = new JObject { ["name"] = "IsInPreWinterSeason" },
                        ["arguments"] = new JArray()
                    }
                },
                ["actions"] = new JArray
                {
                    new JObject
                    {
                        ["expressionType"] = "Call",
                        ["method"] = new JObject { ["name"] = "CreateWorkOrder" },
                        ["arguments"] = new JArray
                        {
                            new JObject
                            {
                                ["expressionType"] = "BasicLiteral",
                                ["kind"] = "STRING",
                                ["value"] = "Furnace Tune-Up"
                            }
                        }
                    },
                    new JObject
                    {
                        ["expressionType"] = "Call",
                        ["method"] = new JObject { ["name"] = "SendCustomerNotification" },
                        ["arguments"] = new JArray
                        {
                            new JObject
                            {
                                ["expressionType"] = "BasicLiteral",
                                ["kind"] = "STRING",
                                ["value"] = "Furnace Check-up Reminder"
                            }
                        }
                    }
                }
            };

            var compiler = new RuleCompiler(
                MethodResolutionOptions.WithExtensionMethods(typeof(MaintenanceContext), typeof(MaintenanceContextExtensions)),
                new LiteralExpressionCompiler()
            );

            var compiledRule = compiler.CompileRule<MaintenanceContext>(ruleJson);

            var customer = new Customer { Name = "John Smith", Email = "john@example.com" };
            var equipment = new Equipment
            {
                EquipmentType = "Furnace",
                InstallDate = new DateTime(2018, 1, 1),
                LastServiceDate = new DateTime(2023, 7, 15)
            };

            var context = new MaintenanceContext
            {
                Customer = customer,
                Equipment = equipment,
                CurrentDate = new DateTime(2025, 9, 1) // In season
            };

            // Act
            compiledRule.EvaluateAndExecute(context);

            // Assert
            Assert.Equal(2, context.Instructions.Count);
            Assert.Contains(context.Instructions, inst => inst is CreateWorkOrderInstruction);
            Assert.Contains(context.Instructions, inst => inst is SendCustomerNotificationInstruction);

            var workOrder = context.Instructions.OfType<CreateWorkOrderInstruction>().First();
            Assert.Equal("Furnace Tune-Up", workOrder.WorkOrderType);

            var notification = context.Instructions.OfType<SendCustomerNotificationInstruction>().First();
            Assert.Equal("Furnace Check-up Reminder", notification.MessageTemplate);
        }
    }
}
