namespace LMAPI.Mcp;

/// <summary>
/// Static registry of all MCP tools exposed by LMAPI.
/// Each tool maps directly to one controller endpoint / service method.
/// </summary>
public static class McpToolDefinitions
{
    public static readonly IReadOnlyList<McpTool> All =
    [
        new McpTool
        {
            Name = "list_devices",
            Description =
                "Returns a paged list of all monitored devices in the LogicMonitor organization. " +
                "Use the filter parameter to narrow results, e.g. alertStatus:\"critical\" or displayName~\"web\".",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    size = new
                    {
                        type = "integer",
                        description = "Number of devices to return (1–1000). Default: 50.",
                        @default = 50
                    },
                    offset = new
                    {
                        type = "integer",
                        description = "Zero-based pagination offset. Default: 0.",
                        @default = 0
                    },
                    filter = new
                    {
                        type = "string",
                        description =
                            "Optional LM v3 filter expression, e.g. alertStatus:\"critical\" or displayName~\"web\"."
                    }
                }
            }
        },

        new McpTool
        {
            Name = "get_device",
            Description =
                "Returns full details for a LogicMonitor monitored device by its integer device ID. " +
                "Includes display name, status, alert status, custom properties, and timestamps.",
            InputSchema = new
            {
                type = "object",
                required = new[] { "deviceId" },
                properties = new
                {
                    deviceId = new
                    {
                        type = "integer",
                        description = "The LogicMonitor integer device ID."
                    }
                }
            }
        },

        new McpTool
        {
            Name = "get_device_events",
            Description =
                "Returns a paged list of events (operational log messages) for a LogicMonitor device. " +
                "Use the filter parameter to narrow by severity, type, or other LM v3 filter expressions.",
            InputSchema = new
            {
                type = "object",
                required = new[] { "deviceId" },
                properties = new
                {
                    deviceId = new
                    {
                        type = "integer",
                        description = "The LogicMonitor integer device ID."
                    },
                    size = new
                    {
                        type = "integer",
                        description = "Number of events to return (1–1000). Default: 50.",
                        @default = 50
                    },
                    offset = new
                    {
                        type = "integer",
                        description = "Zero-based pagination offset. Default: 0.",
                        @default = 0
                    },
                    filter = new
                    {
                        type = "string",
                        description =
                            "Optional LM v3 filter expression, e.g. severity:\"error\" or type:\"agentDownAlert\"."
                    }
                }
            }
        },

        new McpTool
        {
            Name = "get_device_alerts",
            Description =
                "Returns a paged list of active or historical alerts for a LogicMonitor device. " +
                "Use severity filter values: 2=critical, 3=error, 4=warning.",
            InputSchema = new
            {
                type = "object",
                required = new[] { "deviceId" },
                properties = new
                {
                    deviceId = new
                    {
                        type = "integer",
                        description = "The LogicMonitor integer device ID."
                    },
                    size = new
                    {
                        type = "integer",
                        description = "Number of alerts to return (1–1000). Default: 50.",
                        @default = 50
                    },
                    offset = new
                    {
                        type = "integer",
                        description = "Zero-based pagination offset. Default: 0.",
                        @default = 0
                    },
                    filter = new
                    {
                        type = "string",
                        description =
                            "Optional additional LM v3 filter expression ANDed with the device filter, " +
                            "e.g. severity:2 for critical alerts only."
                    }
                }
            }
        },

        new McpTool
        {
            Name = "search_log_events",
            Description =
                "Searches log events in LogicMonitor LM Logs (Log Intelligence). " +
                "Use the filter parameter with LM v3 syntax, e.g. " +
                "_lm.resourceId.system.deviceId:\"42\" to scope logs to a specific device.",
            InputSchema = new
            {
                type = "object",
                properties = new
                {
                    filter = new
                    {
                        type = "string",
                        description =
                            "LM v3 filter expression, e.g. _lm.resourceId.system.deviceId:\"42\"."
                    },
                    size = new
                    {
                        type = "integer",
                        description = "Number of log events to return (1–1000). Default: 50.",
                        @default = 50
                    },
                    offset = new
                    {
                        type = "integer",
                        description = "Zero-based pagination offset. Default: 0.",
                        @default = 0
                    }
                }
            }
        }
    ];
}
