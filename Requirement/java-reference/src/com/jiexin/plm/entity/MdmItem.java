/*     */ package com.jiexin.plm.entity;
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ public class MdmItem
/*     */ {
/*     */   private String new_sap_number;
/*     */   private String new_material_group;
/*     */   private String number;
/*     */   private String name;
/*     */   private String basic_units;
/*     */   private String new_product_code2;
/*     */   
/*     */   public String getNew_sap_number() {
/*  16 */     return this.new_sap_number;
/*     */   }
/*     */   private String industry_sector; private String state; private String factory; private String product_description2; private String material_type; private String parts_mat_type;
/*     */   public void setNew_sap_number(String new_sap_number) {
/*  20 */     this.new_sap_number = new_sap_number;
/*     */   }
/*     */   
/*     */   public String getNew_material_group() {
/*  24 */     return this.new_material_group;
/*     */   }
/*     */   
/*     */   public void setNew_material_group(String new_material_group) {
/*  28 */     this.new_material_group = new_material_group;
/*     */   }
/*     */   
/*     */   public String getNumber() {
/*  32 */     return this.number;
/*     */   }
/*     */   
/*     */   public void setNumber(String number) {
/*  36 */     this.number = number;
/*     */   }
/*     */   
/*     */   public String getName() {
/*  40 */     return this.name;
/*     */   }
/*     */   
/*     */   public void setName(String name) {
/*  44 */     this.name = name;
/*     */   }
/*     */   
/*     */   public String getBasic_units() {
/*  48 */     return this.basic_units;
/*     */   }
/*     */   
/*     */   public void setBasic_units(String basic_units) {
/*  52 */     this.basic_units = basic_units;
/*     */   }
/*     */   
/*     */   public String getNew_product_code2() {
/*  56 */     return this.new_product_code2;
/*     */   }
/*     */   
/*     */   public void setNew_product_code2(String new_product_code2) {
/*  60 */     this.new_product_code2 = new_product_code2;
/*     */   }
/*     */   
/*     */   public String getIndustry_sector() {
/*  64 */     return this.industry_sector;
/*     */   }
/*     */   
/*     */   public void setIndustry_sector(String industry_sector) {
/*  68 */     this.industry_sector = industry_sector;
/*     */   }
/*     */   
/*     */   public String getState() {
/*  72 */     return this.state;
/*     */   }
/*     */   
/*     */   public void setState(String state) {
/*  76 */     this.state = state;
/*     */   }
/*     */   
/*     */   public String getFactory() {
/*  80 */     return this.factory;
/*     */   }
/*     */   
/*     */   public void setFactory(String factory) {
/*  84 */     this.factory = factory;
/*     */   }
/*     */   
/*     */   public String getProduct_description2() {
/*  88 */     return this.product_description2;
/*     */   }
/*     */   
/*     */   public void setProduct_description2(String product_description2) {
/*  92 */     this.product_description2 = product_description2;
/*     */   }
/*     */   
/*     */   public String getMaterial_type() {
/*  96 */     return this.material_type;
/*     */   }
/*     */   
/*     */   public void setMaterial_type(String material_type) {
/* 100 */     this.material_type = material_type;
/*     */   }
/*     */   
/*     */   public String getParts_mat_type() {
/* 104 */     return this.parts_mat_type;
/*     */   }
/*     */   
/*     */   public void setParts_mat_type(String parts_mat_type) {
/* 108 */     this.parts_mat_type = parts_mat_type;
/*     */   }
/*     */ 
/*     */   
/*     */   public MdmItem() {}
/*     */ 
/*     */   
/*     */   public MdmItem(String new_sap_number, String new_material_group, String number, String name, String basic_units, String new_product_code2, String industry_sector, String state, String factory, String product_description2, String material_type, String parts_mat_typ) {
/* 116 */     this.new_sap_number = new_sap_number;
/* 117 */     this.new_material_group = new_material_group;
/* 118 */     this.number = number;
/* 119 */     this.name = name;
/* 120 */     this.basic_units = basic_units;
/* 121 */     this.new_product_code2 = new_product_code2;
/* 122 */     this.industry_sector = industry_sector;
/* 123 */     this.state = state;
/* 124 */     this.factory = factory;
/* 125 */     this.product_description2 = product_description2;
/* 126 */     this.material_type = material_type;
/* 127 */     this.parts_mat_type = parts_mat_typ;
/*     */   }
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */   
/*     */   public String toString() {
/* 141 */     return "MdmItem{new_sap_number='" + this.new_sap_number + '\'' + ", new_material_group='" + this.new_material_group + '\'' + ", number='" + this.number + '\'' + ", name='" + this.name + '\'' + ", basic_units='" + this.basic_units + '\'' + ", new_product_code2='" + this.new_product_code2 + '\'' + ", industry_sector='" + this.industry_sector + '\'' + ", state='" + this.state + '\'' + ", factory='" + this.factory + '\'' + ", product_description2='" + this.product_description2 + '\'' + ", material_type='" + this.material_type + '\'' + ", parts_mat_typ='" + this.parts_mat_type + '\'' + '}';
/*     */   }
/*     */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\plm\entity\MdmItem.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */