import { check } from 'k6';
import { Options } from 'k6/options';
import http from 'k6/http';
import { base_url } from './constants';


export let options: Options = {
    vus: 20,
    duration: '20s'
};


export var create_reminder = () => {
    const url = `${base_url}/APIReminders`
    const req_body = {
        "name": `TEST0001.${__VU}.${__ITER}`,
        "schedule": new Date().toISOString(),
        "command": {
            // "url": "https://test-api.k6.io",
            "url": "http://localhost:5600/health",
            "httpMethod": "GET"
        }
    };
    const params = {
        headers: {
            'Content-Type': 'application/json'
        }
    };
    const res = http.post(url, JSON.stringify(req_body), params);
    check(res, {
        'status is 201': () => res.status === 201,
    });
};
