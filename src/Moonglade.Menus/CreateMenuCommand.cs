﻿using MediatR;
using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Moonglade.Menus
{
    public class CreateMenuCommand : IRequest<Guid>
    {
        public CreateMenuCommand(UpdateMenuRequest request)
        {
            Request = request;
        }

        public UpdateMenuRequest Request { get; set; }
    }

    public class CreateMenuCommandHandler : IRequestHandler<CreateMenuCommand, Guid>
    {
        private readonly IRepository<MenuEntity> _menuRepo;
        private readonly IBlogAudit _audit;

        public CreateMenuCommandHandler(IRepository<MenuEntity> menuRepo, IBlogAudit audit)
        {
            _menuRepo = menuRepo;
            _audit = audit;
        }

        public async Task<Guid> Handle(CreateMenuCommand request, CancellationToken cancellationToken)
        {
            var uid = Guid.NewGuid();
            var menu = new MenuEntity
            {
                Id = uid,
                Title = request.Request.Title.Trim(),
                DisplayOrder = request.Request.DisplayOrder,
                Icon = request.Request.Icon,
                Url = request.Request.Url,
                IsOpenInNewTab = request.Request.IsOpenInNewTab
            };

            if (request.Request.SubMenus is { Length: > 0 })
            {
                var sms = request.Request.SubMenus.Select(p => new SubMenuEntity
                {
                    Id = Guid.NewGuid(),
                    IsOpenInNewTab = p.IsOpenInNewTab,
                    Title = p.Title,
                    Url = p.Url,
                    MenuId = uid
                });

                menu.SubMenus = sms.ToList();
            }

            await _menuRepo.AddAsync(menu);
            await _audit.AddEntry(BlogEventType.Content, BlogEventId.MenuCreated, $"Menu '{uid}' created.");
            return uid;
        }
    }
}
