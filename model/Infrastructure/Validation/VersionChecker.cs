using System.Linq;
using Application.Models;
using Application.Validation.Contracts;
using log4net;

namespace Infrastructure.Validation
{
    public class VersionChecker : IVersionChecker
    {
        /// <summary>
        ///     Initialize Log4net
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(VersionChecker));

        public bool IsHigherVersion(PackageEntry entry, int? dbVersionMajor, int? dbVersionMinor, bool isTvod = false)
        {
            return ValidateVersionMajor(entry, dbVersionMajor, dbVersionMinor, isTvod);
        }

        private bool ValidateVersionMajor(PackageEntry entry, int? dbVersionMajor, int? dbVersionMinor,
            bool isTvod = false)
        {
            var adiVersionMajor = entry.AdiData.Adi.Metadata.AMS.Version_Major;
            var paid = entry.AdiData.Adi.Asset.Metadata.AMS.Asset_ID;
            entry.IsDuplicateIngest = false;
            entry.UpdateVersionFailure = false;

            if (dbVersionMajor >= 0)
            {
                Log.Info(
                    $"[Version Information] DB Version Major: {dbVersionMajor}, ADI Version Major: {adiVersionMajor}");

                if (adiVersionMajor > dbVersionMajor)
                {
                    if (entry.AdiData.Adi.Asset.Asset?
                        .FirstOrDefault()?.Content == null || isTvod)
                    {
                        Log.Info($"Confirmed that package with PAID: {paid} is an update. ");
                        //ensure this is set for media unpack later in workflow
                        entry.IsPackageAnUpdate = true;

                        return true;
                    }

                    Log.Error("Metadata update contains a media section, failing ingest.");
                    return false;
                }

                if (adiVersionMajor == dbVersionMajor)
                {
                    Log.Info("Package Version Major matches DB Version Major, Checking version Minor.");

                    if (ValidateVersionMinor(entry, dbVersionMinor, isTvod))
                    {
                        Log.Info($"Confirmed that package with PAID: {paid} is an update. ");
                        //ensure this is set for media unpack later in workflow
                        entry.IsPackageAnUpdate = true;

                        return true;
                    }

                    entry.IsDuplicateIngest = true;
                    Log.Error(
                        $"Package for PAID: {paid} already exists, duplicate ingest detected! Failing Enhancement.");
                    return false;
                }

                entry.UpdateVersionFailure = true;
                Log.Error(
                    $"Package for PAID: {paid} detected as an update but does not have a higher Version Major! Failing Enhancement.");
                return false;
            }

            Log.Error(
                $"Package for PAID: {paid} detected as an update does not have a database entry for Version Major! Failing Enhancement.");
            return false;
        }

        private bool ValidateVersionMinor(PackageEntry entry, int? dbVersionMinor, bool isTvod = false)
        {
            var adiVersionMinor = entry.AdiData.Adi.Metadata.AMS.Version_Minor;
            var paid = entry.AdiData.Adi.Asset.Metadata.AMS.Asset_ID;
            entry.IsDuplicateIngest = false;
            entry.UpdateVersionFailure = false;

            Log.Info($"[Version Information] DB Version Minor: {dbVersionMinor}, ADI Version Minor: {adiVersionMinor}");

            if (adiVersionMinor > dbVersionMinor)
            {
                if (entry.AdiData.Adi.Asset.Asset?
                    .FirstOrDefault()?.Content == null || isTvod)
                {
                    //ensure this is set for media unpack later in workflow
                    entry.IsPackageAnUpdate = true;

                    return true;
                }

                Log.Error("Metadata update contains a media section, failing ingest.");
                return false;
            }

            if (adiVersionMinor == dbVersionMinor)
            {
                entry.IsDuplicateIngest = true;
                Log.Error($"Package for PAID: {paid} already exists, duplicate ingest detected! Failing Enhancement.");
                return false;
            }

            entry.UpdateVersionFailure = true;
            Log.Error(
                $"Package for PAID: {paid} detected as an update but does not have a higher Version Minor! Failing Enhancement.");
            return false;
        }
    }
}