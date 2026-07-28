using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace AulaComite.Application.Common.Interfaces
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
