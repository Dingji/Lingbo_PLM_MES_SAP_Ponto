/*     */ package com.jiexin.plm.util;
/*     */ 
/*     */ import java.io.File;
/*     */ import java.util.ArrayList;
/*     */ import java.util.HashMap;
/*     */ import java.util.List;
/*     */ import java.util.Map;
/*     */ import org.dom4j.Document;
/*     */ import org.dom4j.Element;
/*     */ import org.dom4j.io.SAXReader;
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ 
/*     */ public class GetStatusXml
/*     */ {
/*  20 */   public static Document document = null;
/*  21 */   public static Document itemInfodocument = null;
/*     */   public static final String ITEMINFOTITLEBLOCK_PATH = "ItemTitleBlockInfo.path";
/*     */   public static final String ITEMINFOPAGETWO_PATH = "ItemPageTwoInfo.path";
/*     */   public static final String ITEMINFOPAGETHREE_PATH = "ItemPageThreeInfo.path";
/*     */   public static final String CHANGEINFO_PATH = "ChangeInfo.path";
/*     */   public static final String AFFECTEDITEMSINFO_PATH = "AffectedItemsInfo.path";
/*     */   public static final String ITEMBOMINFO_PATH = "ItemBOMInfo.path";
/*     */   public static final String PROCESSROUTEINFO_PATH = "ProcessRouteInfo.path";
/*     */   public static final String AFFECTEDBOMSINFO_PATH = "AffectedBOMsInfo.path";
/*     */   public static final String BOMINFOPAGETWO_PATH = "BomPageTwoInfo.path";
/*     */   public static final String BOMINFOPAGETHREE_PATH = "BomPageThreeInfo.path";
/*     */   public static final String ITEMTYPES_PATH = "ItemTypesInfo.path";
/*     */   
/*     */   public static Map<String, Integer> getTitleBlockInfo() {
/*  35 */     document = null;
/*  36 */     itemInfodocument = null;
/*  37 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/*  39 */       if (document == null) {
/*  40 */         SAXReader reader = new SAXReader();
/*  41 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("ItemTitleBlockInfo.path")));
/*     */       } 
/*  43 */       List<Element> fields = itemInfodocument.selectNodes("//class/fields/field");
/*  44 */       for (Element element : fields) {
/*  45 */         String fieldId = element.attributeValue("id");
/*  46 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/*  47 */         String name = element.attributeValue("name");
/*  48 */         map.put(name, id);
/*     */       }
/*     */     
/*  51 */     } catch (Exception e) {
/*  52 */       e.printStackTrace();
/*     */     } 
/*  54 */     return map;
/*     */   }
/*     */   public static Map<String, Integer> getPageTwoInfo() {
/*  57 */     document = null;
/*  58 */     itemInfodocument = null;
/*  59 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/*  61 */       if (document == null) {
/*  62 */         SAXReader reader = new SAXReader();
/*  63 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("ItemPageTwoInfo.path")));
/*     */       } 
/*  65 */       List<Element> fields = itemInfodocument.selectNodes("//class/fields/field");
/*  66 */       for (Element element : fields) {
/*  67 */         String fieldId = element.attributeValue("id");
/*  68 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/*  69 */         String name = element.attributeValue("name");
/*  70 */         map.put(name, id);
/*     */       } 
/*  72 */     } catch (Exception e) {
/*  73 */       document = null;
/*  74 */       e.printStackTrace();
/*     */     } 
/*  76 */     return map;
/*     */   }
/*     */   public static Map<String, Integer> getPageThreeInfo(String className) {
/*  79 */     document = null;
/*  80 */     itemInfodocument = null;
/*  81 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/*  83 */       if (document == null) {
/*  84 */         SAXReader reader = new SAXReader();
/*  85 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("ItemPageThreeInfo.path")));
/*     */       } 
/*  87 */       List<Element> fields = itemInfodocument.selectNodes("//class[@name='" + className + "']/fields/field");
/*  88 */       for (Element element : fields) {
/*  89 */         String fieldId = element.attributeValue("id");
/*  90 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/*  91 */         String name = element.attributeValue("name");
/*  92 */         map.put(name, id);
/*     */       } 
/*  94 */     } catch (Exception e) {
/*  95 */       e.printStackTrace();
/*     */     } 
/*  97 */     return map;
/*     */   }
/*     */   public static Map<String, Integer> getChangeInfo(String className) {
/* 100 */     document = null;
/* 101 */     itemInfodocument = null;
/* 102 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/* 104 */       if (document == null) {
/* 105 */         SAXReader reader = new SAXReader();
/* 106 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("ChangeInfo.path")));
/*     */       } 
/* 108 */       List<Element> fields = itemInfodocument.selectNodes("//class[@name='" + className + "']/fields/field");
/* 109 */       for (Element element : fields) {
/* 110 */         String fieldId = element.attributeValue("id");
/* 111 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/* 112 */         String name = element.attributeValue("name");
/* 113 */         map.put(name, id);
/*     */       } 
/* 115 */     } catch (Exception e) {
/* 116 */       e.printStackTrace();
/*     */     } 
/* 118 */     return map;
/*     */   }
/*     */   public static Map<String, Integer> getAffectedItemsInfo() {
/* 121 */     document = null;
/* 122 */     itemInfodocument = null;
/* 123 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/* 125 */       if (document == null) {
/* 126 */         SAXReader reader = new SAXReader();
/* 127 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("AffectedItemsInfo.path")));
/*     */       } 
/* 129 */       List<Element> fields = itemInfodocument.selectNodes("//class/fields/field");
/* 130 */       for (Element element : fields) {
/* 131 */         String fieldId = element.attributeValue("id");
/* 132 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/* 133 */         String name = element.attributeValue("name");
/* 134 */         map.put(name, id);
/*     */       } 
/* 136 */     } catch (Exception e) {
/* 137 */       e.printStackTrace();
/*     */     } 
/* 139 */     return map;
/*     */   }
/*     */   public static Map<String, Integer> getItemBOMInfo() {
/* 142 */     document = null;
/* 143 */     itemInfodocument = null;
/* 144 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/* 146 */       if (document == null) {
/* 147 */         SAXReader reader = new SAXReader();
/* 148 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("ItemBOMInfo.path")));
/*     */       } 
/* 150 */       List<Element> fields = itemInfodocument.selectNodes("//class/fields/field");
/* 151 */       for (Element element : fields) {
/* 152 */         String fieldId = element.attributeValue("id");
/* 153 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/* 154 */         String name = element.attributeValue("name");
/* 155 */         map.put(name, id);
/*     */       } 
/* 157 */     } catch (Exception e) {
/* 158 */       e.printStackTrace();
/*     */     } 
/* 160 */     return map;
/*     */   }
/*     */   public static Map<String, Integer> getProcessRouteInfo() {
/* 163 */     document = null;
/* 164 */     itemInfodocument = null;
/* 165 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/* 167 */       if (document == null) {
/* 168 */         SAXReader reader = new SAXReader();
/* 169 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("ProcessRouteInfo.path")));
/*     */       } 
/* 171 */       List<Element> fields = itemInfodocument.selectNodes("//class/fields/field");
/* 172 */       for (Element element : fields) {
/* 173 */         String fieldId = element.attributeValue("id");
/* 174 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/* 175 */         String name = element.attributeValue("name");
/* 176 */         map.put(name, id);
/*     */       } 
/* 178 */     } catch (Exception e) {
/* 179 */       e.printStackTrace();
/*     */     } 
/* 181 */     return map;
/*     */   }
/*     */   public static Map<String, Integer> getAffectedBOMsInfo() {
/* 184 */     document = null;
/* 185 */     itemInfodocument = null;
/* 186 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/* 188 */       if (document == null) {
/* 189 */         SAXReader reader = new SAXReader();
/* 190 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("AffectedBOMsInfo.path")));
/*     */       } 
/* 192 */       List<Element> fields = itemInfodocument.selectNodes("//class/fields/field");
/* 193 */       for (Element element : fields) {
/* 194 */         String fieldId = element.attributeValue("id");
/* 195 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/* 196 */         String name = element.attributeValue("name");
/* 197 */         map.put(name, id);
/*     */       } 
/* 199 */     } catch (Exception e) {
/* 200 */       e.printStackTrace();
/*     */     } 
/* 202 */     return map;
/*     */   }
/*     */   
/*     */   public static Map<String, Integer> getBomPageTwoInfo() {
/* 206 */     document = null;
/* 207 */     itemInfodocument = null;
/* 208 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/* 210 */       if (document == null) {
/* 211 */         SAXReader reader = new SAXReader();
/* 212 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("BomPageTwoInfo.path")));
/*     */       } 
/* 214 */       List<Element> fields = itemInfodocument.selectNodes("//class/fields/field");
/* 215 */       for (Element element : fields) {
/* 216 */         String fieldId = element.attributeValue("id");
/* 217 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/* 218 */         String name = element.attributeValue("name");
/* 219 */         map.put(name, id);
/*     */       } 
/* 221 */     } catch (Exception e) {
/* 222 */       document = null;
/* 223 */       e.printStackTrace();
/*     */     } 
/* 225 */     return map;
/*     */   }
/*     */   public static Map<String, Integer> getBomPageThreeInfo(String className) {
/* 228 */     document = null;
/* 229 */     itemInfodocument = null;
/* 230 */     Map<String, Integer> map = new HashMap<>();
/*     */     try {
/* 232 */       if (document == null) {
/* 233 */         SAXReader reader = new SAXReader();
/* 234 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("BomPageThreeInfo.path")));
/*     */       } 
/* 236 */       List<Element> fields = itemInfodocument.selectNodes("//class[@name='" + className + "']/fields/field");
/* 237 */       for (Element element : fields) {
/* 238 */         String fieldId = element.attributeValue("id");
/* 239 */         Integer id = Integer.valueOf(Integer.parseInt(fieldId));
/* 240 */         String name = element.attributeValue("name");
/* 241 */         map.put(name, id);
/*     */       } 
/* 243 */     } catch (Exception e) {
/* 244 */       e.printStackTrace();
/*     */     } 
/* 246 */     return map;
/*     */   }
/*     */   public static List<Element> getItemTypes(String className) {
/* 249 */     document = null;
/* 250 */     itemInfodocument = null;
/* 251 */     List<Element> fields = new ArrayList<>();
/*     */     try {
/* 253 */       if (document == null) {
/* 254 */         SAXReader reader = new SAXReader();
/* 255 */         itemInfodocument = reader.read(new File(PropertiesUtil.getPropertyValue("ItemTypesInfo.path")));
/*     */       } 
/* 257 */       fields = itemInfodocument.selectNodes("//class[@name='" + className + "']/fields/field");
/*     */     }
/* 259 */     catch (Exception e) {
/* 260 */       e.printStackTrace();
/*     */     } 
/* 262 */     return fields;
/*     */   }
/*     */ }


/* Location:              C:\Users\dingj\Desktop\in\123\!\com\jiexin\pl\\util\GetStatusXml.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */