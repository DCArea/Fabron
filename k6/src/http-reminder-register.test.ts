import { check } from 'k6';
import * as execution from 'k6/execution';
import http from 'k6/http';
import { Options } from 'k6/options';
import { apiKey, baseUrl, targetUrl } from './envs';

export let options: Options = {
    discardResponseBodies: true,
    scenarios: {
        contacts: {
            executor: 'shared-iterations',
            vus: 500,
            iterations: 10000,
            maxDuration: '100s',
        }
    },
};

const now = Date.now();
export default () => {
    const scenario = execution.default.scenario;
    const name = `${now / 1000}-${scenario.iterationInTest}`;
    const url = `${baseUrl}/http-reminders/${name}`
    const req_body = {
        "name": name,
        "schedule": new Date(Date.now() + 6 * 60000).toISOString(),
        "command": {
            "url": targetUrl,
            "httpMethod": "GET"
        }
    };
    const params = {
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `ApiKey ${apiKey}`,
        }
    };
    const res = http.put(url, JSON.stringify(req_body), params);
    check(res, {
        'status is 201': () => res.status === 201,
    });
};
