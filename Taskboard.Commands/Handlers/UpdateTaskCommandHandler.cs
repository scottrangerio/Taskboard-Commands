﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Optional;
using Optional.Unsafe;
using Taskboard.Commands.Commands;
using Taskboard.Commands.Enums;
using Taskboard.Commands.Extensions;
using Taskboard.Commands.Repositories;
using Task = Taskboard.Commands.Domain.Task;

namespace Taskboard.Commands.Handlers
{
    public class UpdateTaskCommandHandler : ICommandHander<UpdateTaskCommand>
    {
        private readonly IListRepository repo;

        public UpdateTaskCommandHandler(IListRepository repo)
        {
            this.repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<Option<CommandFailure>> Execute(UpdateTaskCommand command)
        {
            var getResult = await repo.GetById(command.ListId);

            if (!getResult.HasValue)
            {
                return Option.Some(getResult.ExceptionOrFailure().MapToCommandFailure());
            }

            var list = getResult.ValueOrFailure();
            var task = list.Tasks.FirstOrDefault(t => t.Id == command.TaskId);

            if (task == null)
            {
                return Option.Some(CommandFailure.NotFound);
            }

            task.Name = command.Name;
            task.Description = command.Description;

            var replaceResult = await repo.Replace(list);

            return replaceResult.Match(
                error => Option.Some(error.MapToCommandFailure()),
                () => Option.None<CommandFailure>()
            );
        }
    }
}