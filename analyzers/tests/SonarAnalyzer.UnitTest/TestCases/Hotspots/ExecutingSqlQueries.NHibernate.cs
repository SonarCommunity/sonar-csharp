﻿using System;
using System.Linq;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Impl;

namespace Tests.Diagnostics
{
    class Program
    {
        public async Task ISessionMethods(ISession session, string query, string param)
        {
            session.CreateQuery(query);                                                                     // Compliant
            session.CreateQuery(query + param);                                                             // Noncompliant

            session.CreateSQLQuery(query);                                                                  // Compliant
            session.CreateSQLQuery(query + param);                                                          // Noncompliant

            session.Delete(query);                                                                          // Compliant
            session.Delete(query + param);                                                                  // Noncompliant

            await session.DeleteAsync(query);                                                               // Compliant
            await session.DeleteAsync(query + param);                                                       // Noncompliant

            session.GetNamedQuery(query);                                                                   // Compliant
            session.GetNamedQuery(query + param);                                                           // Noncompliant
        }
    }
}
