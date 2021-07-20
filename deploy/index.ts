import * as workloads from "./workloads";
import * as redis from "./redis";
import * as monitoring from "./monitoring";
import { namespace_name, service_name } from "./core";

const redis_config = redis.deploy(namespace_name);
const { deployment, service } = workloads.deploy(redis_config);

monitoring.deploy(service_name, service);

export const image = deployment.spec.template.spec.containers[0].image;
