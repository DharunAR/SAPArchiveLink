﻿using System.ComponentModel;
using TRIM.SDK;

namespace SAPArchiveLink
{
    public interface IArchiveRecord
    {
        List<SapDocumentComponentModel> GetAllComponents();
        SapDocumentComponentModel? GetComponentById(string compId);
        public IRecordSapComponent? FindComponentById(string compId);
        bool HasComponent(string compId);
        Task<List<SapDocumentComponentModel>> ExtractAllComponents(bool extractContent = true);
        Task<SapDocumentComponentModel?> ExtractComponentById(string compId, bool extractContent = true);
        DateTime DateCreated { get; }
        DateTime DateModified { get; }
        int ComponentCount { get; }
        public void Save();
        void AddComponent(string compId, string filePath, string contentType, string charSet, string version);
        void UpdateComponent(IRecordSapComponent component,SapDocumentComponentModel model);
        public void SetRecordMetadata();
        bool DeleteComponent(string compId);
        void DeleteRecord();    
    }

}
