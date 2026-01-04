using Microsoft.Extensions.Options;
using Neptuo.Recollections.Accounts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neptuo.Recollections.Entries
{
    public class PremiumImageSizeValidator : ImageValidator
    {
        private readonly IUserPremiumProvider premiumProvider;
        private readonly StorageOptions configuration;

        public PremiumImageSizeValidator(IUserPremiumProvider premiumProvider, IOptions<StorageOptions> configuration)
            : base(configuration)
        {
            Ensure.NotNull(premiumProvider, "premiumProvider");
            Ensure.NotNull(configuration, "configuration");
            this.premiumProvider = premiumProvider;
            this.configuration = configuration.Value;
        }

        protected override async Task<bool> IsValidSizeAsync(string userId, long fileLength)
        {
            if (configuration.PremiumMaxLength != null)
            {
                bool hasPremium = await premiumProvider.HasPremiumAsync(userId);
                if (hasPremium)
                    return fileLength <= configuration.PremiumMaxLength.Value;
            }

            return await base.IsValidSizeAsync(userId, fileLength);
        }
    }
}
