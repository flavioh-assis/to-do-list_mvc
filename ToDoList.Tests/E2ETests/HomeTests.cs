using System;
using System.Collections.Generic;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ToDoList.App.Data.Context;
using ToDoList.App.Models;
using ToDoList.Tests.Builders;
using ToDoList.Tests.Drivers;
using ToDoList.Tests.E2ETests.Pages;
using ToDoList.Tests.Factories;
using ToDoList.Tests.Utils;
using Xunit;

namespace ToDoList.Tests.E2ETests;

public class HomeTests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly TaskContext _dbContext;
    private readonly WebServerDriver _server;
    private readonly HomePage _page;
    private readonly string _serverUrl;

    private readonly TaskModel _task1Pending;
    private readonly TaskModel _task2Pending;
    private readonly int _totalPendingTask;

    private const string TestDatabase = "db_e2e_home_tests";

    public HomeTests()
    {
        var taskBuilder = new TaskModelBuilder();

        _server = new WebServerDriver();
        _server.Start(TestDatabase);
        _serverUrl = $"{_server.BaseUrl}:{_server.Port}";

        _dbContext = new TaskContextFactory()
            .CreateDbContext(new[] { _server.TestConnectionString });

        var isConnected = DbHelper.CheckDbConnection(_dbContext);
        if (!isConnected)
        {
            Dispose();
            throw new Exception("Failed to connect to database.");
        }

        _task1Pending = taskBuilder
            .WithTitle("Task 1")
            .WithDescription("Description of task 1")
            .Pending()
            .Build();

        _task2Pending = taskBuilder
            .WithTitle("Task 2")
            .WithDescription("Description of task 2")
            .Pending()
            .Build();

        var pendingTasks = new List<TaskModel>
        {
            _task1Pending,
            _task2Pending,
        };

        DbHelper.InsertTasks(_dbContext, pendingTasks).Wait();

        _totalPendingTask = pendingTasks.Count;

        var options = new ChromeOptions
        {
            AcceptInsecureCertificates = true,
        };
        // options.AddArgument("--headless=new");
        _driver = new ChromeDriver(options);

        _page = new HomePage(_driver);
        _page.NavigateToHome();
    }

    [Fact]
    public void ShouldDisplayHeadingText()
    {
        var expectedHeadingText = "Pendentes";

        var heading = _page.Heading();

        heading.Text.Should().Be(expectedHeadingText);
    }

    [Fact]
    public void ShouldDisplayPendingTasksCards()
    {
        var expectedLength = _totalPendingTask;

        var taskCardsElements = _page.Cards();

        taskCardsElements.Count.Should().Be(expectedLength);
    }

    [Fact]
    public void WhenTaskIsCompleted_ShouldRedirectToPageCompleted()
    {
        var taskId = _task1Pending.Id;
        var taskTitle = _task1Pending.Title;
        var expectedUrl = $"{_serverUrl}/Task/Completed";

        _page.CompleteTask(taskTitle);
        _page.ClickOkOnModalComplete(taskId);

        var currentUrl = _page.CurrentUrl();
        currentUrl.Should().Be(expectedUrl);
    }

    [Fact]
    public void WhenTaskIsCompleted_ShouldChangeStatusToCompleted()
    {
        var taskId = _task2Pending.Id;
        var taskTitle = _task2Pending.Title;
        var expectedBodyText = "Status: Concluída";

        _page.CompleteTask(taskTitle);
        _page.ClickOkOnModalComplete(taskId);

        var cardBody = _page.CardBodyByTitle(taskTitle);
        cardBody.Text.Should().Contain(expectedBodyText);
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();

        DbHelper.Dispose(_dbContext);

        _server.Stop().Wait();
    }
}