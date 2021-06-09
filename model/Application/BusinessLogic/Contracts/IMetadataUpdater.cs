using System;
using Application.FileManager.Serialization;
using Application.Models;
using CSharpFunctionalExtensions;

namespace Application.BusinessLogic.Contracts
{
    public interface IMetadataUpdater
    {
        Result CopyPreviouslyEnrichedAssetDataToAdi(AdiData adiData, Guid ingestGuid, bool hasPreviewAsset,
            bool hasPreviousUpdate);
    }
}