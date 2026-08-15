/*    */ package com.jiexin.plm.util;
/*    */ 
/*    */ import java.util.ResourceBundle;
/*    */ 
/*    */ 
/*    */ 
/*    */ public class ConfigUtil
/*    */ {
/*  9 */   private static ResourceBundle rb = null;
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */   
/*    */   public static String getPropertyValue(String key) {
/* 18 */     String value = null;
/* 19 */     if (rb == null) {
/* 20 */       rb = ResourceBundle.getBundle("com.jiexin.plm.resource.config");
/*    */     }
/* 22 */     if (rb.containsKey(key)) {
/* 23 */       value = rb.getString(key).trim();
/*    */     }
/* 25 */     return value;
/*    */   }
/*    */ }


/* Location:              C:\Users\dingj\Desktop\in\123\!\com\jiexin\pl\\util\ConfigUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */