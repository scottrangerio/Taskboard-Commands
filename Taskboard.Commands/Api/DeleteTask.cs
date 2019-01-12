using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SimpleInjector;
using Taskboard.Commands.Commands;
using Taskboard.Commands.Exceptions;
using Taskboard.Commands.Handlers;
using Taskboard.Commands.Repositories;

namespace Taskboard.Commands.Api
{
    public static class DeleteTask
    {
        public static Container Container = BuildContainer();

        [FunctionName(nameof(DeleteTask))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "list/{listid}/task/{taskid}")]
            HttpRequest req, string listid, string taskid)
        {
            try
            {
                var command = new DeleteTaskCommand {ListId = listid, TaskId = taskid};
                var handler = Container.GetInstance<ICommandHander<DeleteTaskCommand>>();

                await handler.Execute(command);

                return new NoContentResult();
            }
            catch (ResourceNotFoundException ex)
            {
                Container.GetInstance<TelemetryClient>().TrackException(ex);

                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                Container.GetInstance<TelemetryClient>().TrackException(ex);

                return new InternalServerErrorResult();
            }
        }

        private static Container BuildContainer()
        {
            var container = new Container();

            container.RegisterSingleton(() => new TelemetryClient
            {
                InstrumentationKey = Environment.GetEnvironmentVariable("AI_INSTRUMENTATIONKEY")
            });
            container.RegisterSingleton<IDocumentClient>(() =>
                new DocumentClient(new Uri(Environment.GetEnvironmentVariable("COSMOS_ENDPOINT")),
                    Environment.GetEnvironmentVariable("COSMOS_KEY")));
            container.Register<IListRepository>(() => new ListRepository(container.GetInstance<IDocumentClient>(),
                Environment.GetEnvironmentVariable("COSMOS_DB"),
                Environment.GetEnvironmentVariable("COSMOS_COLLECTION")));
            container.Register<ICommandHander<DeleteTaskCommand>, DeleteTaskCommandHandler>();

            return container;
        }
    }
}