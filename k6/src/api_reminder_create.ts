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
            iterations: 6000,
            maxDuration: '100s',
        },
    },
};


export var create_reminder = () => {
    const url = `${base_url}/HttpReminders`
    const req_body = {
        "name": `TEST0023.${__VU}.${__ITER}`,
        "schedule": "2021-08-10T08:10:00.000Z",
        "command": {
            "url": "http://stub.dca.svc.cluster.local/noop",
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
