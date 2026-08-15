/*    */ package com.jiexin.plm.util;
/*    */ 
/*    */ import java.util.HashMap;
/*    */ import java.util.Map;
/*    */ import java.util.ResourceBundle;
/*    */ import java.util.Set;
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ public class PropertiesUtil
/*    */ {
/* 13 */   private static ResourceBundle rb = null;
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */   
/*    */   public static String getPropertyValue(String key) {
/* 21 */     String env = ConfigUtil.getPropertyValue("env");
/* 22 */     String fileName = "";
/* 23 */     if (env.equals("dev")) {
/* 24 */       fileName = "config-dev";
/* 25 */     } else if (env.equals("test")) {
/* 26 */       fileName = "config-test";
/* 27 */     } else if (env.equals("pro")) {
/* 28 */       fileName = "config-pro";
/*    */     } else {
/* 30 */       throw new RuntimeException("配置文件config未知！！！");
/*    */     } 
/* 32 */     String value = null;
/* 33 */     if (rb == null) {
/* 34 */       rb = ResourceBundle.getBundle("com.jiexin.plm.resource." + fileName);
/*    */     }
/* 36 */     if (rb.containsKey(key)) {
/* 37 */       value = rb.getString(key).trim();
/*    */     }
/* 39 */     return value;
/*    */   }
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */   
/*    */   public static Map<String, String> getProperties() {
/* 48 */     Map<String, String> properties = new HashMap<>();
/* 49 */     if (rb == null) {
/* 50 */       rb = ResourceBundle.getBundle("com.jiexin.plm.resource.config");
/*    */     }
/* 52 */     Set<String> keys = rb.keySet();
/* 53 */     for (String key : keys) {
/* 54 */       String value = rb.getString(key).trim();
/* 55 */       properties.put(key, value);
/*    */     } 
/* 57 */     return properties;
/*    */   }
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */   
/*    */   public static int getAgileID(String resourceKey) throws Exception {
/* 69 */     int id = -1;
/* 70 */     String updatorID = getPropertyValue(resourceKey);
/* 71 */     if (updatorID != null && !updatorID.equals("") && 
/* 72 */       updatorID.matches("^(([0-9]+)?)$")) {
/* 73 */       id = Integer.parseInt(updatorID);
/*    */     }
/*    */     
/* 76 */     if (id == -1) {
/* 77 */       throw new Exception(resourceKey + " in config.properties should be a Integer.");
/*    */     }
/* 79 */     return id;
/*    */   }
/*    */ }


/* Location:              C:\Users\dingj\Desktop\in\123\!\com\jiexin\pl\\util\PropertiesUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */