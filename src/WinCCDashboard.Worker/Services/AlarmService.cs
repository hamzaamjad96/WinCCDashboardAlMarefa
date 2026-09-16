using System;
using System.Collections.Generic;
using System.Text;

namespace WinCCReportingWorkerMarefa.Services
{
    public class AlarmService
    {
        private readonly GraphQLService _graphQLService;
        private readonly SqlService _sqlService;
        private readonly ILogger<AlarmService> _logger;

        public AlarmService(
            GraphQLService graphQLService,
            SqlService sqlService,
            ILogger<AlarmService> logger)
        {
            _graphQLService = graphQLService;
            _sqlService = sqlService;
            _logger = logger;
        }
    }
}
