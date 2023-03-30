﻿using Moonglade.Data;
using Moonglade.Data.Entities;
using Moonglade.Data.Infrastructure;
using Moonglade.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Moonglade.Auth
{
    public interface ILocalAccountService
    {
        int Count();
        Task<Account> GetAsync(Guid id);
        Task<IReadOnlyList<Account>> GetAllAsync();
        Task<Guid> ValidateAsync(string username, string inputPassword);
        Task LogSuccessLoginAsync(Guid id, string ipAddress);
        bool Exist(string username);
        Task<Guid> CreateAsync(string username, string clearPassword);
        Task UpdatePasswordAsync(Guid id, string clearPassword);
        Task DeleteAsync(Guid id);
    }

    public class LocalAccountService : ILocalAccountService
    {
        private readonly IRepository<LocalAccountEntity> _accountRepo;
        private readonly IBlogAudit _audit;

        public LocalAccountService(
            IRepository<LocalAccountEntity> accountRepo,
            IBlogAudit audit)
        {
            _accountRepo = accountRepo;
            _audit = audit;
        }

        public int Count()
        {
            return _accountRepo.Count();
        }

        public async Task<Account> GetAsync(Guid id)
        {
            var entity = await _accountRepo.GetAsync(id);
            var item = EntityToAccountModel(entity);
            return item;
        }

        public Task<IReadOnlyList<Account>> GetAllAsync()
        {
            var list = _accountRepo.SelectAsync(p => new Account
            {
                Id = p.Id,
                CreateTimeUtc = p.CreateTimeUtc,
                LastLoginIp = p.LastLoginIp,
                LastLoginTimeUtc = p.LastLoginTimeUtc,
                Username = p.Username
            });

            return list;
        }

        public async Task<Guid> ValidateAsync(string username, string inputPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username), "value must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(inputPassword))
            {
                throw new ArgumentNullException(nameof(inputPassword), "value must not be empty.");
            }

            var account = await _accountRepo.GetAsync(p => p.Username == username);
            if (account is null) return Guid.Empty;

            var valid = account.PasswordHash == Helper.HashPassword(inputPassword.Trim());
            return valid ? account.Id : Guid.Empty;
        }

        public async Task LogSuccessLoginAsync(Guid id, string ipAddress)
        {
            var entity = await _accountRepo.GetAsync(id);
            if (entity is not null)
            {
                entity.LastLoginIp = ipAddress.Trim();
                entity.LastLoginTimeUtc = DateTime.UtcNow;
                await _accountRepo.UpdateAsync(entity);
            }
        }

        public bool Exist(string username)
        {
            var exist = _accountRepo.Any(p => p.Username == username.ToLower());
            return exist;
        }

        public async Task<Guid> CreateAsync(string username, string clearPassword)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username), "value must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(clearPassword))
            {
                throw new ArgumentNullException(nameof(clearPassword), "value must not be empty.");
            }

            var uid = Guid.NewGuid();
            var account = new LocalAccountEntity
            {
                Id = uid,
                CreateTimeUtc = DateTime.UtcNow,
                Username = username.ToLower().Trim(),
                PasswordHash = Helper.HashPassword(clearPassword.Trim())
            };

            await _accountRepo.AddAsync(account);
            await _audit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsAccountCreated, $"Account '{account.Id}' created.");

            return uid;
        }

        public async Task UpdatePasswordAsync(Guid id, string clearPassword)
        {
            if (string.IsNullOrWhiteSpace(clearPassword))
            {
                throw new ArgumentNullException(nameof(clearPassword), "value must not be empty.");
            }

            var account = await _accountRepo.GetAsync(id);
            if (account is null)
            {
                throw new InvalidOperationException($"LocalAccountEntity with Id '{id}' not found.");
            }

            account.PasswordHash = Helper.HashPassword(clearPassword);
            await _accountRepo.UpdateAsync(account);

            await _audit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsAccountPasswordUpdated, $"Account password for '{id}' updated.");
        }

        public async Task DeleteAsync(Guid id)
        {
            var account = await _accountRepo.GetAsync(id);
            if (account is null)
            {
                throw new InvalidOperationException($"LocalAccountEntity with Id '{id}' not found.");
            }

            await _accountRepo.DeleteAsync(id);
            await _audit.AddEntry(BlogEventType.Settings, BlogEventId.SettingsDeleteAccount, $"Account '{id}' deleted.");
        }

        private static Account EntityToAccountModel(LocalAccountEntity entity)
        {
            if (entity is null) return null;

            return new()
            {
                Id = entity.Id,
                CreateTimeUtc = entity.CreateTimeUtc,
                LastLoginIp = entity.LastLoginIp.Trim(),
                LastLoginTimeUtc = entity.LastLoginTimeUtc.GetValueOrDefault(),
                Username = entity.Username.Trim()
            };
        }
    }
}
