/*    */ package com.jiexin.plm.util;
/*    */ 
/*    */ import java.util.UUID;
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ public class NumberUtil
/*    */ {
/*    */   public static String getUUID32() {
/* 17 */     return UUID.randomUUID().toString().replace("-", "").toLowerCase();
/*    */   }
/*    */   
/*    */   public static void main(String[] args) {}
/*    */ }


/* Location:              {{SOURCE_PATH}}\com\jiexin\pl\\util\NumberUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */