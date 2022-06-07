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
            vus: 1000,
            iterations: 30000,
            maxDuration: '100s',
        }
    },
};

const now = Date.now();
export default () => {
    const scenario = execution.default.scenario;
    const name = `${now / 1000}-${scenario.iterationInTest}`;
    const url = `${baseUrl}/timedevents/${name}`
    const req_body = {
        "schedule": new Date(Date.now() + 6 * 60000).toISOString(),
        "template": {
            "data": {
                "foo": "bar"
            }
        },
        "routingDestination": "http://stub.dca.svc.cluster.local/cloudevents"
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
