using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace PMS.Services.Services.StudentPortal.Devices
{
    public class StudentDeviceService : IStudentDeviceService
    {
        private readonly UnitOfWork<PMS.EF.PMSEntities> uow;

        public StudentDeviceService(UnitOfWork<PMS.EF.PMSEntities> _uow)
        {
            uow = _uow;
        }

        public bool RegisterDevice(int userId, int personId, string deviceToken, string platform, string deviceId)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(deviceToken))
            {
                return false;
            }

            var token = deviceToken.Trim();
            var now = DateTime.Now;
            var existingId = uow.Context.Database.SqlQuery<long?>(
                "SELECT ID FROM StudentDeviceToken WHERE DeviceToken = @deviceToken AND IsEnable = 1",
                new SqlParameter("@deviceToken", token)).FirstOrDefault();

            if (existingId.HasValue)
            {
                uow.Context.Database.ExecuteSqlCommand(
                    @"UPDATE StudentDeviceToken
                      SET UserId = @userId,
                          PersonId = @personId,
                          Platform = @platform,
                          DeviceId = @deviceId,
                          IsActive = 1,
                          LastLoginAt = @now,
                          UpdatedAt = @now
                      WHERE ID = @id",
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@personId", (object)personId ?? DBNull.Value),
                    new SqlParameter("@platform", (object)platform ?? DBNull.Value),
                    new SqlParameter("@deviceId", (object)deviceId ?? DBNull.Value),
                    new SqlParameter("@now", now),
                    new SqlParameter("@id", existingId.Value));
            }
            else
            {
                uow.Context.Database.ExecuteSqlCommand(
                    @"INSERT INTO StudentDeviceToken
                      (UserId, PersonId, DeviceToken, Platform, DeviceId, IsActive, LastLoginAt, CreatedAt, IsEnable)
                      VALUES (@userId, @personId, @deviceToken, @platform, @deviceId, 1, @now, @now, 1)",
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@personId", (object)personId ?? DBNull.Value),
                    new SqlParameter("@deviceToken", token),
                    new SqlParameter("@platform", (object)platform ?? DBNull.Value),
                    new SqlParameter("@deviceId", (object)deviceId ?? DBNull.Value),
                    new SqlParameter("@now", now));
            }

            return true;
        }

        public bool UnregisterDevice(int userId, string deviceToken)
        {
            if (userId <= 0)
            {
                return false;
            }

            var now = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(deviceToken))
            {
                uow.Context.Database.ExecuteSqlCommand(
                    @"UPDATE StudentDeviceToken
                      SET IsActive = 0, UpdatedAt = @now
                      WHERE UserId = @userId AND DeviceToken = @deviceToken AND IsEnable = 1",
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@deviceToken", deviceToken.Trim()),
                    new SqlParameter("@now", now));
            }
            else
            {
                uow.Context.Database.ExecuteSqlCommand(
                    @"UPDATE StudentDeviceToken
                      SET IsActive = 0, UpdatedAt = @now
                      WHERE UserId = @userId AND IsEnable = 1 AND IsActive = 1",
                    new SqlParameter("@userId", userId),
                    new SqlParameter("@now", now));
            }

            return true;
        }

        public List<string> GetActiveDeviceTokensByPersonId(int personId)
        {
            if (personId <= 0)
            {
                return new List<string>();
            }

            return uow.Context.Database.SqlQuery<string>(
                @"SELECT DeviceToken
                  FROM StudentDeviceToken
                  WHERE PersonId = @personId AND IsEnable = 1 AND IsActive = 1",
                new SqlParameter("@personId", personId)).ToList();
        }

        public List<string> GetActiveDeviceTokensByUserId(int userId)
        {
            if (userId <= 0)
            {
                return new List<string>();
            }

            return uow.Context.Database.SqlQuery<string>(
                @"SELECT DeviceToken
                  FROM StudentDeviceToken
                  WHERE UserId = @userId AND IsEnable = 1 AND IsActive = 1",
                new SqlParameter("@userId", userId)).ToList();
        }

        public void DeactivateDeviceToken(string deviceToken)
        {
            if (string.IsNullOrWhiteSpace(deviceToken))
            {
                return;
            }

            uow.Context.Database.ExecuteSqlCommand(
                @"UPDATE StudentDeviceToken
                  SET IsActive = 0, UpdatedAt = @now
                  WHERE DeviceToken = @deviceToken AND IsEnable = 1",
                new SqlParameter("@deviceToken", deviceToken.Trim()),
                new SqlParameter("@now", DateTime.Now));
        }
    }
}
