
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Contracts;
using Fabron.Mando;
using Fabron.Models;

namespace Fabron;

public interface IFabronQuerier
{
    Task<List<Job<TCommand, TResult>>> FindJobByOwnerAsync<TCommand, TResult>(
        string @namespace,
        OwnerReference owner,
        int skip = 0,
        int take = 20
    ) where TCommand : ICommand<TResult>;

    Task<List<Job<TCommand, TResult>>> FindJobByLabelsAsync<TCommand, TResult>(
        string @namespace,
        Dictionary<string, string> labels,
        int skip = 0,
        int take = 20) where TCommand : ICommand<TResult>;

    Task<List<CronJob<TCommand>>> FindCronJobByLabelsAsync<TCommand>(
        string @namespace,
        Dictionary<string, string> labels,
        int skip = 0,
        int take = 20) where TCommand : ICommand;
}
