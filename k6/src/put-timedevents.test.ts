import { check } from 'k6';
import * as execution from 'k6/execution';
import http from 'k6/http';
import { Options } from 'k6/options';
import { apiKey, baseUrl, targetUrl } from './envs';

export let options: Options = {
    discardResponseBodies: true,
    scenarios: {
        // contacts: {
        //     executor: 'shared-iterations',
        //     vus: 2000,
        //     iterations: 30000,
        //     maxDuration: '100s',
        // },
        contacts: {
            executor: 'constant-vus',
            vus: 2000,
            duration: '2m',
        },
    },
};


export function setup() {
    const now = Date.now();
    const schedule = new Date(now + 1 * 60000).toISOString();
    // const schedule = "2022-06-10T08:11:00Z";
    console.log(schedule);
    return { now, schedule };
}

export default function ({ now, schedule }: { now: number, schedule: string }) {
    const scenario = execution.default.scenario;
    const name = `${now}-${scenario.iterationInTest}`;

    now = Date.now();
    schedule = new Date(now + 5 * 60000).toISOString();

    const url = `${baseUrl}/timedevents/${name}`
    const req_body = {
        "schedule": schedule,
        "template": {
            "data": {
                "foo": "bar"
            }
        },
        "routingDestination": targetUrl
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
