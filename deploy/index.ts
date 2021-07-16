import * as workloads from "./workloads";
import * as redis from "./redis";
import { namespace_name } from "./core";

const redis_config = redis.deploy(namespace_name);
const { deployment } = workloads.deploy(redis_config);

export const image = deployment.spec.template.spec.containers[0].image;
