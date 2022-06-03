﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Core.Entities;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Business.Tokenables;
using Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Interfaces;
using Bit.Core.Repositories;
using Bit.Core.Tokens;

namespace Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Cloud
{
    public class SendSponsorshipOfferCommand : ISendSponsorshipOfferCommand
    {
        private readonly IUserRepository _userRepository;
        private readonly IFamiliesForEnterpriseMailer _mailer;
        private readonly IDataProtectorTokenFactory<OrganizationSponsorshipOfferTokenable> _tokenFactory;

        public SendSponsorshipOfferCommand(IUserRepository userRepository,
            IFamiliesForEnterpriseMailer mailer,
            IDataProtectorTokenFactory<OrganizationSponsorshipOfferTokenable> tokenFactory)
        {
            _userRepository = userRepository;
            _mailer = mailer;
            _tokenFactory = tokenFactory;
        }

        public async Task BulkSendSponsorshipOfferAsync(Organization sponsoringOrg, IEnumerable<OrganizationSponsorship> sponsorships)
        {
            var invites = new List<(OrganizationSponsorship, bool, string)>();
            foreach (var sponsorship in sponsorships)
            {
                var user = await _userRepository.GetByEmailAsync(sponsorship.OfferedToEmail);
                invites.Add((sponsorship, user != null, _tokenFactory.Protect(new OrganizationSponsorshipOfferTokenable(sponsorship))));
            }

            await _mailer.BulkSendFamiliesForEnterpriseOfferEmailAsync(sponsoringOrg, invites);
        }

        public async Task SendSponsorshipOfferAsync(OrganizationSponsorship sponsorship, Organization sponsoringOrg)
        {
            var user = await _userRepository.GetByEmailAsync(sponsorship.OfferedToEmail);
            var isExistingAccount = user != null;

            await _mailer.SendFamiliesForEnterpriseOfferEmailAsync(sponsoringOrg, sponsorship,
                isExistingAccount, _tokenFactory.Protect(new OrganizationSponsorshipOfferTokenable(sponsorship)));
        }

        public async Task SendSponsorshipOfferAsync(Organization sponsoringOrg, OrganizationUser sponsoringOrgUser,
            OrganizationSponsorship sponsorship)
        {
            if (sponsoringOrg == null)
            {
                throw new BadRequestException("Cannot find the requested sponsoring organization.");
            }

            if (sponsoringOrgUser == null || sponsoringOrgUser.Status != OrganizationUserStatusType.Confirmed)
            {
                throw new BadRequestException("Only confirmed users can sponsor other organizations.");
            }

            if (sponsorship == null || sponsorship.OfferedToEmail == null)
            {
                throw new BadRequestException("Cannot find an outstanding sponsorship offer for this organization.");
            }

            await SendSponsorshipOfferAsync(sponsorship, sponsoringOrg);
        }
    }
}
