﻿using HomeOwnerBestie.Common;
using HomeOwnerBestie.LeadData.SQL.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace HomeOwnerBestie.LeadData.DataProvider
{
    public class SQLLeadDataProvider : ILeadDataProvider
    {
        HomeOwnerBestieDBContext homeOwnerBestieDBContext = null;

        public SQLLeadDataProvider(HomeOwnerBestieDBContext homeOwnerBestieDBContext)
        {
            this.homeOwnerBestieDBContext = homeOwnerBestieDBContext;
        }

        public string AddUser(HOBAppUser user)
        {
            var existingUser = FindUser(user.Email);

            // update
            if (existingUser != null)
            {
                return UpdateUser(existingUser, user);
            }

            // add if not present
            if (existingUser == null)
            {
                return AddNewUser(user);
            }

            return string.Empty;
        }

        string UpdateUser(AppUsers existingUser, HOBAppUser hobUser)
        {
            existingUser.DateModified = DateTime.Now;
            existingUser.FirstName = hobUser.FirstName;
            existingUser.LastName = hobUser.LastName;
            existingUser.Phone = hobUser.Phone;
            homeOwnerBestieDBContext.SaveChanges();
            return existingUser.UserId;
        }

        AppUsers FindUser(string email)
        {
            return homeOwnerBestieDBContext.AppUsers
                    .Where(item => item.Email == email)
                    .FirstOrDefault();
        }

        UserAddresses FindAddress(string userid, Address address)
        {
            return homeOwnerBestieDBContext.UserAddresses
                                       .Where(item => item.City == address.City
                                           && item.State == address.State && item.Street == address.Street
                                           && item.UserId == userid && item.Zip == address.Zip)
                                       .FirstOrDefault();
        }

        string AddNewAddress(string existingUserId, Address address)
        {
            var newAddressGuid = Guid.NewGuid().ToString();
            homeOwnerBestieDBContext.UserAddresses.Add(new UserAddresses()
            {
                City = address.City,
                County = address.County,
                State = address.State,
                AddressId = newAddressGuid,
                DateCreated = DateTime.Now,
                Street = address.Street,
                UserId = existingUserId,
                Zip = address.Zip,
            });
            homeOwnerBestieDBContext.SaveChanges();
            return newAddressGuid;
        }

        string AddNewUser(HOBAppUser user)
        {
            var newGuid = Guid.NewGuid().ToString();
            homeOwnerBestieDBContext.AppUsers.Add(new AppUsers()
            {
                DateCreated = DateTime.Now,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                UserId = newGuid
            });
            homeOwnerBestieDBContext.SaveChanges();
            return newGuid;
        }

        public string AddRentValuationRecord(HOBAppUser user, Address address, RentValuationData rentValuationData)
        {
            // check if the user is already there
            AppUsers existingUser = FindUser(user.Email);

            // if this user not found, then add.
            string existingUserId = string.Empty;
            if (existingUser == null) existingUserId = AddNewUser(user);
            else
            {
                // also update the user
                UpdateUser(existingUser, user);
                existingUserId = existingUser.UserId;
            }

            // check if this address is already there. Or else add.
            var existingAddress = FindAddress(existingUserId, address);
            string existingAddressId = string.Empty;
            if (existingAddress == null)
                existingAddressId = AddNewAddress(existingUserId, address);
            else existingAddressId = existingAddress.AddressId;

            // we always add the rent valuation record everytime as it will keep changing as per market fluctuation
            var newRentValuationRecordGuid = Guid.NewGuid().ToString();
            homeOwnerBestieDBContext.RentValuationReports.Add(new RentValuationReports()
            {
                AverageMonthlyRent = rentValuationData.AverageMonthlyRent,
                HomeOwnerSpecifiedRent = rentValuationData.AverageMonthlyRent,
                IsRentEstimateAvailable = rentValuationData.IsRentEstimateAvailable,
                RentValuationRecordId = newRentValuationRecordGuid,
                ValuationRentHigh = rentValuationData.ValuationRentHigh,
                ValuationRentLow = rentValuationData.ValuationRentLow,
                ValueChangedIn30Days = rentValuationData.ValueChangedIn30Days,
                DateCreated = DateTime.Now,
                UserId = existingUserId,
                AddressId = existingAddressId
            });
            homeOwnerBestieDBContext.SaveChanges();

            return newRentValuationRecordGuid;
        }

        public string UpdateHomeOwnerSpecifiedRent(string userId, decimal homeOwnerSpecifiedRent)
        {
            if (string.IsNullOrEmpty(userId)) return string.Empty;

            // Get the last created rent valuation record by this user
            var lastCreatedRentValuationRecord = 
                    homeOwnerBestieDBContext.RentValuationReports
                           .Where(item => item.UserId == userId)
                           .OrderByDescending(item => item.DateCreated )
                           .FirstOrDefault();

            // Update the record with homeOwnerSpecifiedRent
            lastCreatedRentValuationRecord.HomeOwnerSpecifiedRent = homeOwnerSpecifiedRent;
            return lastCreatedRentValuationRecord.RentValuationRecordId;
        }

        public RentValuationData FindRentValuationRecord(string rentValuationRecordId)
        {
            var rentValuationReport = homeOwnerBestieDBContext.RentValuationReports
                                                   .Where(item => item.RentValuationRecordId == rentValuationRecordId)
                                                   .FirstOrDefault();
            if (rentValuationReport != null)
                return new RentValuationData() {
                    AverageMonthlyRent = (decimal)rentValuationReport.AverageMonthlyRent,
                    HomeOwnerSpecifiedRent = (decimal)rentValuationReport.HomeOwnerSpecifiedRent,
                    IsRentEstimateAvailable = (bool)rentValuationReport.IsRentEstimateAvailable,
                    ValuationRentHigh = (decimal)rentValuationReport.ValuationRentHigh,
                    ValuationRentLow = (decimal)rentValuationReport.ValuationRentLow,
                    ValueChangedIn30Days = (decimal)rentValuationReport.ValueChangedIn30Days,
                };
            else return null;
        }

        public Address GetAddressFromRentValuationRecordId(string rentValuationRecordId)
        {
            var rentValuationReport = homeOwnerBestieDBContext.RentValuationReports
                                                   .Where(item => item.RentValuationRecordId == rentValuationRecordId)
                                                   .FirstOrDefault();

            if (rentValuationReport != null)
            {
                var address = homeOwnerBestieDBContext.UserAddresses
                                                       .Where(item => item.AddressId == rentValuationReport.AddressId)
                                                       .FirstOrDefault();

                return new Address() {
                    City = address.City,
                    County = address.County,
                    State = address.State,
                    Zip = address.Zip,
                    Street = address.Street
                };
            }
            return null;
        }

        public string GetUserIdFromEmail(string email)
        {
            var user = FindUser(email);

            if (user != null) return user.UserId;
            else return null;
        }

    }
}
