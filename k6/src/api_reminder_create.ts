import { check } from 'k6';
import { Options } from 'k6/options';
import http from 'k6/http';
import { base_url } from './constants';


export let options: Options = {
    // vus: 200,
    // duration: '60s',
    discardResponseBodies: true,
    scenarios: {
        contacts: {
            executor: 'shared-iterations',
            vus: 300,
            iterations: 8000,
            maxDuration: '100s',
        },
        // contacts: {
        //     executor: 'constant-arrival-rate',
        //     rate: 300, // 200 RPS, since timeUnit is the default 1s
        //     duration: '20s',
        //     preAllocatedVUs: 50,
        //     maxVUs: 1000,
        // },
    },
};


export var create_reminder = () => {
    const url = `${base_url}/Reminders`
    const req_body = {
        "name": `${__VU}.${__ITER}`,
        // "schedule": new Date(Date.now() + 6*60000).toISOString(),
        // "schedule": "2021-08-10T16:10:00.000Z",
        "command": {
            "url": "http://stub.dca.svc.cluster.local/noop",
            "httpMethod": "GET"
        }
    };
    const params = {
        headers: {
            'Content-Type': 'application/json',
            'x-app-id': 'TEST0200'
        }
    };
    const res = http.post(url, JSON.stringify(req_body), params);
    check(res, {
        'status is 201': () => res.status === 201,
    });
};
