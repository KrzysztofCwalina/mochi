// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.Data.Tables;

namespace mochi.fx;

public class ToDoClient
{
    readonly TableServiceClient _tableService;
    readonly TableClient TasksTable;

    public ToDoClient(SettingsClient settings)
    {
        var storageConnectionString = settings.GetSecret("mochi-storage-cs");
        _tableService = new TableServiceClient(storageConnectionString);
        TasksTable = _tableService.GetTableClient("todos");
    }

    public IReadOnlyList<ToDo> Get(string? assignedTo = default)
    {
        var tasks = TasksTable.Query<ToDo>().ToList();
        return tasks;
    }

    public void Add(ToDo toDo)
    {
        TasksTable.AddEntity(toDo);
    }
}

public class ToDo : ITableEntity
{
    public string Task { get; set; }
    public string AssignedTo { get; set; }
    public int Priority { get; set; } = 2;

    public string PartitionKey { get; set; } = "todo";
    public string RowKey { get; set; } = Guid.NewGuid().ToString();
    public DateTimeOffset? Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public ETag ETag { get; set; }
}