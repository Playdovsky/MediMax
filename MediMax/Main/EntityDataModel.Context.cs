﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class MediMaxEntities : DbContext
    {
        public MediMaxEntities()
            : base("name=MediMaxEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<tbl_Leki> tbl_Leki { get; set; }
        public virtual DbSet<tbl_Recepta> tbl_Recepta { get; set; }
        public virtual DbSet<tbl_ReceptaZalecenia> tbl_ReceptaZalecenia { get; set; }
        public virtual DbSet<tbl_Rola> tbl_Rola { get; set; }
        public virtual DbSet<tbl_Uzytkownik> tbl_Uzytkownik { get; set; }
        public virtual DbSet<database_firewall_rules> database_firewall_rules { get; set; }
        public virtual DbSet<tbl_StanMagazynowy> tbl_StanMagazynowy { get; set; }
    }
}
