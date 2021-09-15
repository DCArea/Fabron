﻿
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobQuerier
    {
        Task<IEnumerable<Job>> GetJobByLabel(string labelName, string labelValue);
        Task<IEnumerable<Job>> GetJobByLabels(IEnumerable<(string, string)> labels);
        Task<IEnumerable<CronJob>> GetCronJobByLabel(string labelName, string labelValue);
        Task<IEnumerable<CronJob>> GetCronJobByLabels(IEnumerable<(string, string)> labels);
    }

}