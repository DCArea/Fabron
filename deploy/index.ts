import * as workloads from "./workloads";
import * as workloads_qz from "./workloads-qz";
import * as redis from "./redis";
import * as pgsql from "./pgsql";
// import * as mongodb from "./mongodb";
import * as elasticsearch from "./elasticsearch";
import * as monitoring from "./monitoring";
import { namespace_name, service_name } from "./core";

// const redis_config = redis.deploy(namespace_name);
const pgsql_config = pgsql.deploy(namespace_name);
// const mongodb_config = mongodb.deploy(namespace_name);
// const es_config = elasticsearch.deploy(namespace_name);
// const { deployment, service } = workloads.deploy(redis_config, mongodb_config, es_config);
const { deployment, service } = workloads_qz.deploy(pgsql_config);

monitoring.deploy(service_name, service);

export const image = deployment.spec.template.spec.containers[0].image;
