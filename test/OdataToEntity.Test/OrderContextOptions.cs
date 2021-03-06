﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OdataToEntity.Test.Model
{
    internal static class OrderContextOptions
    {
        private sealed class ZStateManager : StateManager
        {
            public ZStateManager(StateManagerDependencies dependencies) : base(dependencies)
            {

            }
            protected override async Task<int> SaveChangesAsync(IReadOnlyList<InternalEntityEntry> entriesToSave, CancellationToken cancellationToken = default)
            {
                UpdateTemporaryKey(entriesToSave);
                int count = await base.SaveChangesAsync(entriesToSave, cancellationToken).ConfigureAwait(false);
                return count;
            }
            internal static void UpdateTemporaryKey(IReadOnlyList<InternalEntityEntry> entries)
            {
                foreach (InternalEntityEntry entry in entries)
                    foreach (IKey key in entry.EntityType.GetKeys())
                        foreach (IProperty property in key.Properties)
                            if (entry.HasTemporaryValue(property))
                            {
                                int id = (int)entry.GetCurrentValue(property);
                                entry.SetProperty(property, -id, false);
                            }
            }

        }

        public static DbContextOptions Create(bool useRelationalNulls, String databaseName)
        {
            var optionsBuilder = new DbContextOptionsBuilder<OrderContext>();
            optionsBuilder.UseInMemoryDatabase(databaseName);
            optionsBuilder.ReplaceService<IStateManager, ZStateManager>();
            return optionsBuilder.Options;
        }
    }
}
