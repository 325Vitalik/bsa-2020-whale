﻿using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whale.DAL.Models;
using Whale.DAL.Settings;

namespace Whale.Shared.Extentions
{
    public static class UserExtentions
    {
        public static async Task<User> LoadAvatarAsync(this User user, BlobStorageSettings settings)
        {
            if(user.LinkType == LinkTypeEnum.External) return user;

            var response = await GetAllBlobsAsync(settings);

            return GetLinkFromStorage(user, response);
        }

        public static async Task<IEnumerable<T>> LoadAvatarsAsync<T>(this IEnumerable<T> source, BlobStorageSettings settings, Func<T, User> predicate)
        {
            var response = await GetAllBlobsAsync(settings);

            return source.Select(t =>
            {
                predicate(t).GetLinkFromStorage(response);
                return t;
            });
        }

        public static async Task<IEnumerable<User>> LoadAvatarsAsync(this IEnumerable<User> source, BlobStorageSettings settings)
        {
            var response = await GetAllBlobsAsync(settings);

            return source.Select(u => u.GetLinkFromStorage(response));
        }

        private static async Task<BlobResultSegment> GetAllBlobsAsync(BlobStorageSettings settings)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(settings.ConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(settings.ImageContainerName);
            await SetPublicContainerPermissionsAsync(container);
            return await container.ListBlobsSegmentedAsync(null);
        }

        private static User GetLinkFromStorage(this User user, BlobResultSegment response)
        {
            if (user.LinkType == LinkTypeEnum.External) return user;

            var blob = response.Results.FirstOrDefault(x => x.Uri.Segments.Contains(user.AvatarUrl));
            if (blob != null)
            {
                user.AvatarUrl = blob.Uri.AbsoluteUri;
            }
            return user;
        }

        private static async Task SetPublicContainerPermissionsAsync(CloudBlobContainer container)
        {
            await container.CreateIfNotExistsAsync();
            BlobContainerPermissions permissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            };
            await container.SetPermissionsAsync(permissions);
        }
    }
}
