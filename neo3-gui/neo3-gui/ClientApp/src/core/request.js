import axios from 'axios'
import { message } from 'antd'

const request = async (method , params) => {
    // 默认Axios Post方法
    const url = 'http://localhost:8081';
    if( method === ""){
        message.error("method null");
        return;
    }
    var options = Object.assign(
        {
            "id":"1",
            "method": method,
        },
        { "params": {...params} }
    )
    
    return await axios.post(url,options);
}

/**
* 封装post请求
* @param { String } method 请求方法
* @param { Object } params 请求参数
*/
const post = (method , params) => {
    return request(method , params)
}

export { post }
export default request