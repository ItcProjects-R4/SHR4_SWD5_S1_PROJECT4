using Eventify.Domain.Constants;
using Eventify.Domain.Entities;
using Eventify.Domain.Enums;
using EventifyPro.DAL.AppDatabase;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventifyPro.DAL.Seeders;

public static class DataSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles =
        [
            RoleNames.Admin,
            RoleNames.Organizer,
            RoleNames.Attendee,
            RoleNames.Scanner
        ];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    public static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string adminEmail = "eventifyproitcr4@gmail.com";
        const string adminUserName = "eventifypro_admin";
        const string adminPassword = "Eventifypro_itc@1234";
        const string adminName = "Super Admin";

        var existing = await userManager.FindByEmailAsync(adminEmail)
            ?? await userManager.FindByNameAsync(adminUserName);
        if (existing is not null) return;

        var admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            FullName = adminName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to seed admin user: {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(admin);
            var errors = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign admin role: {errors}");
        }
    }

    public static async Task SeedEventsAndCategoriesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<EventifyDbContext>();

        // 1. Seed Organizer User
        const string organizerEmail = "organizer@eventifypro.com";
        const string organizerUserName = "eventifypro_organizer";
        const string organizerPassword = "Eventifypro_itc@1234";
        const string organizerName = "Premium Organizer";

        var organizer = await userManager.FindByEmailAsync(organizerEmail)
            ?? await userManager.FindByNameAsync(organizerUserName);
        if (organizer == null)
        {
            organizer = new ApplicationUser
            {
                UserName = organizerUserName,
                Email = organizerEmail,
                FullName = organizerName,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(organizer, organizerPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(organizer, RoleNames.Organizer);
            }
            else
            {
                var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to seed organizer user: {errors}");
            }
        }

        // 2. Seed Categories (Check existence for each category by Name)
        var categoryData = new List<Category>
        {
            new() { Name = "Music Concerts", Description = "Live music, festivals, and concerts.", CreatedAt = DateTime.UtcNow },
            new() { Name = "Tech Conferences", Description = "Developer workshops and technology summits.", CreatedAt = DateTime.UtcNow },
            new() { Name = "Sports & Fitness", Description = "Marathons, tournaments, and fitness camps.", CreatedAt = DateTime.UtcNow },
            new() { Name = "Art & Design", Description = "Gallery exhibitions and creative showcases.", CreatedAt = DateTime.UtcNow },
            new() { Name = "Business & Finance", Description = "Corporate networking and financial seminars.", CreatedAt = DateTime.UtcNow }
        };

        foreach (var cat in categoryData)
        {
            if (!await context.Categories.AnyAsync(c => c.Name == cat.Name))
            {
                await context.Categories.AddAsync(cat);
            }
        }
        await context.SaveChangesAsync();

        // 3. Seed Events (Check existence for each event by Title)
        var musicCat = await context.Categories.FirstAsync(c => c.Name == "Music Concerts");
        var techCat = await context.Categories.FirstAsync(c => c.Name == "Tech Conferences");
        var sportsCat = await context.Categories.FirstAsync(c => c.Name == "Sports & Fitness");
        var artCat = await context.Categories.FirstAsync(c => c.Name == "Art & Design");

        // Event 1
        var event1 = await context.Events.Include(e => e.TicketTypes).FirstOrDefaultAsync(e => e.Title == "Cairo Summer Beat 2026");
        if (event1 == null)
        {
            event1 = new Event
            {
                Title = "Cairo Summer Beat 2026",
                OrganizerId = organizer.Id,
                CategoryId = musicCat.Id,
                CreatedAt = DateTime.UtcNow,
                TicketTypes = new List<TicketType>()
            };
            await context.Events.AddAsync(event1);
        }
        event1.ImageUrl = "https://images.unsplash.com/photo-1506157786151-b8491531f063?auto=format&fit=crop&w=800&q=80";
        event1.Description = @"Experience the hottest outdoor music festival of the summer under the stars in Cairo. Featuring top local and international DJs, live band performances, and visual art installations.

What to expect:
- Multiple stages with different music genres (Electronic, House, Pop, and Rock).
- Premium food courts and beverage stations offering local and global cuisines.
- Interactive light shows and laser displays that come alive after sunset.
- Lounge areas and VIP decks for a comfortable experience.

Grab your friends, get your tickets, and prepare for a night of dancing, laughter, and unforgettable summer beats!";
        event1.StartDate = DateTime.UtcNow.AddDays(5);
        event1.EndDate = DateTime.UtcNow.AddDays(5).AddHours(6);
        event1.Location = "Zamalek Outdoor Stage";
        event1.City = "Cairo";
        event1.Status = EventStatus.Published;
        event1.IsFeatured = true;
        event1.MaxCapacity = 1000;
        if (!event1.TicketTypes.Any())
        {
            event1.TicketTypes.Add(new() { Name = "Regular Pass", Price = 300, TotalQuantity = 800, SoldQuantity = 0, Description = "General admission access to the festival.", CreatedAt = DateTime.UtcNow });
            event1.TicketTypes.Add(new() { Name = "VIP Lounge Access", Price = 1000, TotalQuantity = 200, SoldQuantity = 0, Description = "Premium view, private bar, and welcome drink.", CreatedAt = DateTime.UtcNow });
        }

        // Event 2
        var event2 = await context.Events.Include(e => e.TicketTypes).FirstOrDefaultAsync(e => e.Title == "NextGen AI & Cloud Summit");
        if (event2 == null)
        {
            event2 = new Event
            {
                Title = "NextGen AI & Cloud Summit",
                OrganizerId = organizer.Id,
                CategoryId = techCat.Id,
                CreatedAt = DateTime.UtcNow,
                TicketTypes = new List<TicketType>()
            };
            await context.Events.AddAsync(event2);
        }
        event2.ImageUrl = "https://images.unsplash.com/photo-1540575467063-178a50c2df87?auto=format&fit=crop&w=800&q=80";
        event2.Description = @"Join leading industry experts, researchers, and developers to discuss the future of Artificial Intelligence, cloud architecture, and modern web development technologies.

Summit Highlights:
- Keynote speeches from AI pioneers and tech leaders from top global firms.
- Interactive panel discussions on cloud security, scalability, and serverless hosting.
- Dedicated hands-on workshops for building LLM-based applications.
- Networking sessions to connect with developers, founders, and investors.

Whether you're a senior architect, a software engineer, or a tech enthusiast, this summit will equip you with the knowledge and connections to lead in the age of AI.";
        event2.StartDate = DateTime.UtcNow.AddDays(15);
        event2.EndDate = DateTime.UtcNow.AddDays(16);
        event2.Location = "Alexandria Library Auditorium";
        event2.City = "Alexandria";
        event2.Status = EventStatus.Published;
        event2.IsFeatured = true;
        event2.MaxCapacity = 500;
        if (!event2.TicketTypes.Any())
        {
            event2.TicketTypes.Add(new() { Name = "Attendee Pass", Price = 150, TotalQuantity = 450, SoldQuantity = 0, Description = "Access to all talks and keynote sessions.", CreatedAt = DateTime.UtcNow });
            event2.TicketTypes.Add(new() { Name = "Pro Workshop Pass", Price = 600, TotalQuantity = 50, SoldQuantity = 0, Description = "Includes attendee pass + hands-on coding labs.", CreatedAt = DateTime.UtcNow });
        }

        // Event 3
        var event3 = await context.Events.Include(e => e.TicketTypes).FirstOrDefaultAsync(e => e.Title == "Pyramids Sunset Marathon 2026");
        if (event3 == null)
        {
            event3 = new Event
            {
                Title = "Pyramids Sunset Marathon 2026",
                OrganizerId = organizer.Id,
                CategoryId = sportsCat.Id,
                CreatedAt = DateTime.UtcNow,
                TicketTypes = new List<TicketType>()
            };
            await context.Events.AddAsync(event3);
        }
        event3.ImageUrl = "https://images.unsplash.com/photo-1502224562085-639556652f33?auto=format&fit=crop&w=800&q=80";
        event3.Description = @"Run alongside history at the beautiful Giza plateau during sunset. This unique athletic event brings together professional runners and amateur enthusiasts from around the world.

Event Package Includes:
- Official marathon runner kit (premium dry-fit t-shirt, bag, and race bib).
- High-precision chip timing for accurate race metrics.
- Hydration stations and medical support every 2.5 kilometers.
- Finisher medal and post-race celebration with live entertainment.

Feel the magic of running past the ancient Pyramids of Giza as the sun dips below the horizon. Choose from 5K, 10K, or half-marathon options!";
        event3.StartDate = DateTime.UtcNow.AddDays(30).AddHours(16);
        event3.EndDate = DateTime.UtcNow.AddDays(30).AddHours(20);
        event3.Location = "Giza Pyramids Complex";
        event3.City = "Giza";
        event3.Status = EventStatus.Published;
        event3.IsFeatured = true;
        event3.MaxCapacity = 2000;
        if (!event3.TicketTypes.Any())
        {
            event3.TicketTypes.Add(new() { Name = "Standard Runner", Price = 250, TotalQuantity = 1800, SoldQuantity = 0, Description = "Access to race, running shirt, medal.", CreatedAt = DateTime.UtcNow });
            event3.TicketTypes.Add(new() { Name = "Elite Runner Kit", Price = 500, TotalQuantity = 200, SoldQuantity = 0, Description = "Premium shirt, headwear, and priority starting zone.", CreatedAt = DateTime.UtcNow });
        }

        // Event 4
        var event4 = await context.Events.Include(e => e.TicketTypes).FirstOrDefaultAsync(e => e.Title == "Contemporary Fine Art Gallery");
        if (event4 == null)
        {
            event4 = new Event
            {
                Title = "Contemporary Fine Art Gallery",
                OrganizerId = organizer.Id,
                CategoryId = artCat.Id,
                CreatedAt = DateTime.UtcNow,
                TicketTypes = new List<TicketType>()
            };
            await context.Events.AddAsync(event4);
        }
        event4.ImageUrl = "https://images.unsplash.com/photo-1460661419201-fd4cecdf8a8b?auto=format&fit=crop&w=800&q=80";
        event4.Description = @"Explore unique visual arts, immersive installations, and abstract sculptures from rising local and international artists. Hosted in a modern, quiet gallery setting in Cairo.

Exhibition Details:
- Curated collections focused on identity, technology, and environmental change.
- Artist talk sessions where creators share the concepts and techniques behind their work.
- Live art demonstration sessions and clay sculpting workshops.
- Complimentary refreshments and catalog booklets for all VIP ticket holders.

Take a peaceful walk through the creative expressions of modern artists and experience the power of contemporary fine arts.";
        event4.StartDate = DateTime.UtcNow.AddDays(8);
        event4.EndDate = DateTime.UtcNow.AddDays(12);
        event4.Location = "El Sawy Culturewheel";
        event4.City = "Cairo";
        event4.Status = EventStatus.Published;
        event4.MaxCapacity = 300;
        if (!event4.TicketTypes.Any())
        {
            event4.TicketTypes.Add(new() { Name = "Free Ticket", Price = 0, TotalQuantity = 250, SoldQuantity = 0, Description = "Free general admission (requires registration).", CreatedAt = DateTime.UtcNow });
            event4.TicketTypes.Add(new() { Name = "Exhibition Tour + Catalog", Price = 120, TotalQuantity = 50, SoldQuantity = 0, Description = "Guided tour with the curator and artist catalog.", CreatedAt = DateTime.UtcNow });
        }

        await context.SaveChangesAsync();

        // 4. Automatically publish any existing unpublished events to ensure they show up on the browse page
        var unpublishedEvents = await context.Events
            .Where(e => e.Status != EventStatus.Published)
            .ToListAsync();

        if (unpublishedEvents.Any())
        {
            foreach (var ev in unpublishedEvents)
            {
                ev.Status = EventStatus.Published;
            }
            await context.SaveChangesAsync();
        }

        // 5. Update old events (IDs 1 to 10) with images and descriptions if they are missing or short
        var oldEvents = await context.Events.Where(e => e.Id >= 1 && e.Id <= 10).ToListAsync();
        if (oldEvents.Any())
        {
            var images = new string[]
            {
                "https://images.unsplash.com/photo-1501281668745-f7f57925c3b4?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1511578314322-379afb476865?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1492684223066-81342ee5ff30?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1475721027785-f74eccf877e2?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1531058020387-3be344559be6?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1515187029135-18ee286d815b?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1517245386807-bb43f82c33c4?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1485827404703-89b55fcc595e?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1527529482837-4698179dc6ce?auto=format&fit=crop&w=800&q=80",
                "https://images.unsplash.com/photo-1505232458627-5c27df76b529?auto=format&fit=crop&w=800&q=80"
            };

            var descriptions = new string[]
            {
                @"Step into an evening of excitement and community connection. This premier local event features interactive workshops, custom culinary options, and excellent networking spaces for all participants.
                
What's included:
- Guest speakers from various professional fields.
- Curated buffet with soft drinks and snacks.
- Hands-on learning labs and Q&A sessions.
                
Register today and don't miss this opportunity to connect with peers and learn from industry leaders!",
                
                @"Discover the latest trends in technology, digital transformation, and business innovation. Our expert panels will explore key strategies to scale operations, secure systems, and automate workflows.
                
Agenda:
- Opening remarks on global industry shifts.
- Core breakout tracks for developers and executives.
- Networking cocktails and interactive booths.
                
Perfect for founders, leaders, and curious professionals seeking to build the next generation of software products.",
                
                @"Celebrate the spirit of music and artistic expression. This lively gathering brings together local songwriters, instrumentalists, and visual artists for a multi-stage showcase of talents.
                
Key features:
- Outdoor acoustic sessions and full band sets.
- Local handmade crafts market and art exhibits.
- Food truck village with diverse culinary tastes.
                
Bring your family and friends for a relaxing, fun-filled weekend celebration of culture!",
                
                @"Get ready for an energetic day of sports, athletic training, and wellness coaching. Designed for all fitness levels, from beginner runners to seasoned athletes.
                
What's included:
- High-intensity interval training (HIIT) warmups.
- Guided runs and basic posture alignment workshops.
- Organic refreshments, fruit cups, and energy bars.
                
Lace up your sneakers and join us for a healthy morning full of positive energy."
            };

            for (int i = 0; i < oldEvents.Count; i++)
            {
                var ev = oldEvents[i];
                
                if (string.IsNullOrEmpty(ev.ImageUrl) || ev.ImageUrl.Contains("placeholder") || ev.ImageUrl.Length < 10)
                {
                    ev.ImageUrl = images[i % images.Length];
                }
                
                if (string.IsNullOrEmpty(ev.Description) || ev.Description.Length < 150)
                {
                    var descPattern = descriptions[i % descriptions.Length];
                    ev.Description = $"Welcome to {ev.Title}!\n\n" + descPattern;
                }
            }
            await context.SaveChangesAsync();
        }

        // 6. Ensure the three main seeded events have IsFeatured = true set in the database
        var featuredTitles = new[] { "Cairo Summer Beat 2026", "NextGen AI & Cloud Summit", "Pyramids Sunset Marathon 2026" };
        var eventsToFeature = await context.Events.Where(e => featuredTitles.Contains(e.Title)).ToListAsync();
        foreach (var ev in eventsToFeature)
        {
            ev.IsFeatured = true;
        }
        await context.SaveChangesAsync();
    }
}
