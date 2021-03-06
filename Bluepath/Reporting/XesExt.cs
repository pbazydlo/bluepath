﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Xml.Serialization;

// 
// This source code was auto-generated by xsd, Version=4.0.30319.33440.
// 


/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
public partial class xesextension {
    
    private AttributableType logField;
    
    private AttributableType traceField;
    
    private AttributableType eventField;
    
    private AttributableType metaField;
    
    private string nameField;
    
    private string prefixField;
    
    private string uriField;
    
    /// <remarks/>
    public AttributableType log {
        get {
            return this.logField;
        }
        set {
            this.logField = value;
        }
    }
    
    /// <remarks/>
    public AttributableType trace {
        get {
            return this.traceField;
        }
        set {
            this.traceField = value;
        }
    }
    
    /// <remarks/>
    public AttributableType @event {
        get {
            return this.eventField;
        }
        set {
            this.eventField = value;
        }
    }
    
    /// <remarks/>
    public AttributableType meta {
        get {
            return this.metaField;
        }
        set {
            this.metaField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="NCName")]
    public string name {
        get {
            return this.nameField;
        }
        set {
            this.nameField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="NCName")]
    public string prefix {
        get {
            return this.prefixField;
        }
        set {
            this.prefixField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="anyURI")]
    public string uri {
        get {
            return this.uriField;
        }
        set {
            this.uriField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
public partial class AttributableType {
    
    private AttributeType[] itemsField;
    
    private ItemsChoiceType[] itemsElementNameField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("boolean", typeof(AttributeType))]
    [System.Xml.Serialization.XmlElementAttribute("container", typeof(AttributeType))]
    [System.Xml.Serialization.XmlElementAttribute("date", typeof(AttributeType))]
    [System.Xml.Serialization.XmlElementAttribute("float", typeof(AttributeType))]
    [System.Xml.Serialization.XmlElementAttribute("id", typeof(AttributeType))]
    [System.Xml.Serialization.XmlElementAttribute("int", typeof(AttributeType))]
    [System.Xml.Serialization.XmlElementAttribute("list", typeof(AttributeType))]
    [System.Xml.Serialization.XmlElementAttribute("string", typeof(AttributeType))]
    [System.Xml.Serialization.XmlChoiceIdentifierAttribute("ItemsElementName")]
    public AttributeType[] Items {
        get {
            return this.itemsField;
        }
        set {
            this.itemsField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("ItemsElementName")]
    [System.Xml.Serialization.XmlIgnoreAttribute()]
    public ItemsChoiceType[] ItemsElementName {
        get {
            return this.itemsElementNameField;
        }
        set {
            this.itemsElementNameField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
public partial class AttributeType {
    
    private AliasType[] aliasField;
    
    private string keyField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("alias")]
    public AliasType[] alias {
        get {
            return this.aliasField;
        }
        set {
            this.aliasField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="Name")]
    public string key {
        get {
            return this.keyField;
        }
        set {
            this.keyField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
public partial class AliasType {
    
    private string mappingField;
    
    private string nameField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute(DataType="NCName")]
    public string mapping {
        get {
            return this.mappingField;
        }
        set {
            this.mappingField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name {
        get {
            return this.nameField;
        }
        set {
            this.nameField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.33440")]
[System.SerializableAttribute()]
[System.Xml.Serialization.XmlTypeAttribute(IncludeInSchema=false)]
public enum ItemsChoiceType {
    
    /// <remarks/>
    boolean,
    
    /// <remarks/>
    container,
    
    /// <remarks/>
    date,
    
    /// <remarks/>
    @float,
    
    /// <remarks/>
    id,
    
    /// <remarks/>
    @int,
    
    /// <remarks/>
    list,
    
    /// <remarks/>
    @string,
}
