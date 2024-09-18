using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Neptuo.Events.Handlers;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Accounts.Events;

namespace Neptuo.Recollections.Entries.Events.Handlers;

public class ServiceProviderEventHandler<T> : IEventHandler<T>
{
    private readonly IServiceProvider services;

    public ServiceProviderEventHandler(IServiceProvider services)
    {
        Ensure.NotNull(services, "services");
        this.services = services;
    }

    public async Task HandleAsync(T payload)
    {
        using var scope = services.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<T>>();
        foreach (var handler in handlers)
            await handler.HandleAsync(payload);
    }
}

public class UserHandler : IEventHandler<UserRegistered>
{
    private readonly UserBeingService userBeings;

    public UserHandler(UserBeingService userBeings)
    {
        Ensure.NotNull(userBeings, "userBeings");
        this.userBeings = userBeings;
    }

    public async Task HandleAsync(UserRegistered payload)
    {
        await userBeings.EnsureAsync(payload.UserId);
    }
}

public class UserBeingService
{
    private readonly DataContext db;
    private readonly UserManager<User> users;

    public UserBeingService(DataContext db, UserManager<User> users)
    {
        Ensure.NotNull(db, "db");
        Ensure.NotNull(users, "users");
        this.db = db;
        this.users = users;
    }

    public async Task EnsureAsync(string userId)
    {
        var being = await db.Beings.SingleOrDefaultAsync(b => b.Id == userId);
        if (being == null)
        {
            var user = await users.FindByIdAsync(userId);
            being = new Being()
            {
                Id = userId,
                Name = user.UserName,
                Icon = "user",
                UserId = userId,
                Created = user.Created
            };

            db.Beings.Add(being);
            await db.SaveChangesAsync();
        }
    }
}