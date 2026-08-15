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


/* Location:              C:\Users\dingj\Desktop\in\123\!\com\jiexin\pl\\util\NumberUtil.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */