using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Nexora.Core.Entities
{
    public class ESignatureTemplateField : IEntity, ITenantEntity, ISoftDelete
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int TemplateId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public string? DefaultValue { get; set; }
        public string? ValidationRules { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime Timestamp { get; set; }
        
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        
        public string? Options { get; set; } // JSON string for dropdown/checkbox options
        public string? AssignedRole { get; set; } // Role that can fill this field
        public string? Placeholder { get; set; }
        public string? HelpText { get; set; }
        public bool IsReadOnly { get; set; }
        public string? ConditionalLogic { get; set; } // JSON string for conditional display logic
        public string? Description { get; set; }
        public int PageNumber { get; set; } = 1;
        
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
        
        public virtual ESignatureTemplate Template { get; set; } = null!;
    }
}
