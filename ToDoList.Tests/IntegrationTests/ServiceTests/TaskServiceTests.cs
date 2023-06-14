using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using ToDoList.App.Data.Context;
using ToDoList.App.Models;
using ToDoList.App.Repository;
using ToDoList.App.Services;
using ToDoList.Test.Builders;
using ToDoList.Test.Factories;
using Xunit;

namespace ToDoList.Tests.IntegrationTests.ServiceTests;

public class TaskServiceTests : IDisposable
{
    private readonly TaskContext _dbContext;
    private readonly TaskService _taskService;
    private readonly TaskModelBuilder _taskBuilder;
    
    private readonly TaskModel _task1Pending;
    private readonly TaskModel _task2Completed;

    public TaskServiceTests()
    {
        _taskBuilder = new TaskModelBuilder();
        
        _dbContext = new TaskContextFactory()
            .CreateDbContext(Array.Empty<string>());
        _dbContext.Database.EnsureCreated();

        var taskRepository = new TaskRepository(_dbContext);
        _taskService = new TaskService(taskRepository);

        _task1Pending = _taskBuilder.Pending().Build();
        _task2Completed = _taskBuilder.Completed().Build();

        _dbContext.Add(_task1Pending);
        _dbContext.Add(_task2Completed);
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetAll_ShouldReturnListTaskModelType()
    {
        var expectedResultType = typeof(List<TaskModel>);

        var result = await _taskService.GetAll();

        result.Should().BeOfType(expectedResultType);
    }

    [Fact]
    public async Task GetAll_ShouldReturnAllTasks()
    {
        var expectedResult = new List<TaskModel>()
        {
            _task1Pending,
            _task2Completed,
        };

        var result = await _taskService.GetAll();

        result.Should().BeEquivalentTo(expectedResult);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}