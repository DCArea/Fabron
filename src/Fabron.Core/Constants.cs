using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fabron
{
    public static class FabronConstants
    {
        public const string Prefix = "fabron.io/";
        public static class LabelNames
        {

            public const string DisplayName = Prefix + "display-name";

            //public const string OwnerId = Prefix + "owner-id";
            public const string OwnerId = Prefix + "owner-id";
            public const string OwnerKey = Prefix + "owner-key";

            public const string OwnerType = Prefix + "owner-type";

            public const string CronIndex = Prefix + "cron-index";
        }

        public static class OwnerTypes
        {
            public const string CronJob = "cronjob";
        }

        public const string CronItemKeyTemplate = Prefix + "cronjobs/{0}/items/{1}";
    }

}
