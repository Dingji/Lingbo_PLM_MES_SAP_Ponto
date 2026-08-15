/*    */ package com.jiexin.plm.util;
/*    */ 
/*    */ import com.alibaba.fastjson.JSON;
/*    */ import com.alibaba.fastjson.JSONObject;
/*    */ import com.alibaba.fastjson.serializer.SerializerFeature;
/*    */ import java.util.Map;
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ 
/*    */ public class JsonUtils
/*    */ {
/*    */   public static Map<String, Object> jsonToMap(String str) throws Exception {
/* 17 */     JSONObject jsonObj = new JSONObject();
/* 18 */     Map<String, Object> map = (Map<String, Object>)JSONObject.parseObject(str, Map.class);
/* 19 */     return map;
/*    */   }
/*    */   
/*    */   public static Map<String, Object> entityToMap(Object obj) throws Exception {
/* 23 */     String str = JSON.toJSONString(obj, new SerializerFeature[] { SerializerFeature.WriteMapNullValue });
/* 24 */     return jsonToMap(str);
/*    */   }
/*    */ 
/*    */ 
/*    */ 
/*    */   
/*    */   public static String mapToJson(Map<String, ?> map) {
/* 31 */     return JSON.toJSONString(map, new SerializerFeature[] { SerializerFeature.WriteNullStringAsEmpty }).toString();
/*    */   }
/*    */ }


/* Location:              C:\Users\dingj\Desktop\in\123\!\com\jiexin\pl\\util\JsonUtils.class
 * Java compiler version: 8 (52.0)
 * JD-Core Version:       1.1.3
 */