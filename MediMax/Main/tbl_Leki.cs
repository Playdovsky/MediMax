//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Main
{
    using System;
    using System.Collections.Generic;
    
    public partial class tbl_Leki
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public tbl_Leki()
        {
            this.tbl_Recepta = new HashSet<tbl_Recepta>();
            this.tbl_StanMagazynowy = new HashSet<tbl_StanMagazynowy>();
            this.tbl_Zamowienia = new HashSet<tbl_Zamowienia>();
            this.tbl_ZapotrzebowanieLeku = new HashSet<tbl_ZapotrzebowanieLeku>();
            this.tbl_Sprzedaz = new HashSet<tbl_Sprzedaz>();
        }
    
        public int Id { get; set; }
        public string Nazwa { get; set; }
        public string Typ { get; set; }
        public bool CzyNaRecepte { get; set; }
        public Nullable<decimal> Cena { get; set; }
        public int IloscWOpakowaniu { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbl_Recepta> tbl_Recepta { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbl_StanMagazynowy> tbl_StanMagazynowy { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbl_Zamowienia> tbl_Zamowienia { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbl_ZapotrzebowanieLeku> tbl_ZapotrzebowanieLeku { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<tbl_Sprzedaz> tbl_Sprzedaz { get; set; }
    }
}
